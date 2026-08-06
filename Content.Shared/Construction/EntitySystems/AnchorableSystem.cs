using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration.Logs;
using Content.Shared.Examine;
using Content.Shared.Construction.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.Database;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;
using Content.Shared.Movement.Pulling.Components;
using Content.Shared.Movement.Pulling.Systems;
using Content.Shared.Popups;
using Content.Shared.Tools.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;
using Robust.Shared.Utility;
using SharedToolSystem = Content.Shared.Tools.Systems.SharedToolSystem;

namespace Content.Shared.Construction.EntitySystems;

public sealed partial class AnchorableSystem : EntitySystem
{
    [Dependency] private ISharedAdminLogManager _adminLogger = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private PullingSystem _pulling = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedToolSystem _tool = default!;
    [Dependency] private SharedTransformSystem _transformSystem = default!;
    [Dependency] private TagSystem _tagSystem = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;

    private EntityQuery<PhysicsComponent> _physicsQuery;

    public readonly ProtoId<TagPrototype> Unstackable = "Unstackable";

    public override void Initialize()
    {
        base.Initialize();

        _physicsQuery = GetEntityQuery<PhysicsComponent>();

        SubscribeLocalEvent<AnchorableComponent, InteractUsingEvent>(OnInteractUsing,
            before: new[] { typeof(ItemSlotsSystem) }, after: new[] { typeof(SharedConstructionSystem) });
        SubscribeLocalEvent<AnchorableComponent, TryAnchorCompletedEvent>(OnAnchorComplete);
        SubscribeLocalEvent<AnchorableComponent, TryUnanchorCompletedEvent>(OnUnanchorComplete);
        SubscribeLocalEvent<AnchorableComponent, ExaminedEvent>(OnAnchoredExamine);
        SubscribeLocalEvent<AnchorableComponent, ComponentStartup>(OnAnchorStartup);
        SubscribeLocalEvent<AnchorableComponent, AnchorStateChangedEvent>(OnAnchorStateChange);
    }

    private void OnAnchorStartup(EntityUid uid, AnchorableComponent comp, ComponentStartup args)
    {
        _appearance.SetData(uid, AnchorVisuals.Anchored, Transform(uid).Anchored);
    }

    private void OnAnchorStateChange(EntityUid uid, AnchorableComponent comp, AnchorStateChangedEvent args)
    {
        _appearance.SetData(uid, AnchorVisuals.Anchored, args.Anchored);
    }

    /// <summary>
    ///     Tries to unanchor the entity.
    /// </summary>
    /// <returns>true if unanchored, false otherwise</returns>
    private void TryUnAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, false))
            return;

        // Log unanchor attempt (server only)
        _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to unanchor {ToPrettyString(uid):entity} from {transform.Coordinates:targetlocation}");

        _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryUnanchorCompletedEvent());
    }

    private void OnInteractUsing(EntityUid uid, AnchorableComponent anchorable, InteractUsingEvent args)
    {
        if (args.Handled)
            return;

        // If the used entity doesn't have a tool, return early.
        if (!TryComp(args.Used, out ToolComponent? usedTool) || !_tool.HasQuality(args.Used, anchorable.Tool, usedTool))
            return;

        args.Handled = true;
        TryToggleAnchor(uid, args.User, args.Used, anchorable, usingTool: usedTool);
    }

    private void OnAnchoredExamine(EntityUid uid, AnchorableComponent component, ExaminedEvent args)
    {
        var isAnchored = Comp<TransformComponent>(uid).Anchored;

        if (isAnchored && (component.Flags & AnchorableFlags.Unanchorable) == 0x0)
            return;

        if (!isAnchored && (component.Flags & AnchorableFlags.Anchorable) == 0x0)
            return;

        var messageId = isAnchored ? "examinable-anchored" : "examinable-unanchored";
        args.PushMarkup(Loc.GetString(messageId, ("target", uid)));
    }

    private void OnUnanchorComplete(EntityUid uid, AnchorableComponent component, TryUnanchorCompletedEvent args)
    {
        if (args.Cancelled || args.Used is not { } used)
            return;

        UnanchorEntity((uid, component), args.User, used);
    }

    public void UnanchorEntity(Entity<AnchorableComponent> ent, EntityUid user, EntityUid used)
    {
        var xform = Transform(ent);

        RaiseLocalEvent(ent, new BeforeUnanchoredEvent(user, used));
        _transformSystem.Unanchor(ent, xform);
        RaiseLocalEvent(ent, new UserUnanchoredEvent(user, used));

        _popup.PopupEntity(Loc.GetString("anchorable-unanchored"), ent, user);

        _adminLogger.Add(
            LogType.Unanchor,
            LogImpact.Low,
            $"{ToPrettyString(user):user} unanchored {ToPrettyString(ent):anchored} using {ToPrettyString(used):using}"
        );
    }

    private void OnAnchorComplete(EntityUid uid, AnchorableComponent component, TryAnchorCompletedEvent args)
    {
        if (args.Cancelled || args.Used is not { } used)
            return;

        AnchorEntity((uid, component), args.User, used);
    }

    public void AnchorEntity(Entity<AnchorableComponent> ent, EntityUid user, EntityUid used)
    {
        var xform = Transform(ent);
        if (TryComp<PhysicsComponent>(ent, out var anchorBody) &&
            !TileFree(xform.Coordinates, anchorBody))
        {
            _popup.PopupEntity(Loc.GetString("anchorable-occupied"), ent, user);
            return;
        }

        // Snap rotation to cardinal (multiple of 90)
        var rot = xform.LocalRotation;
        xform.LocalRotation = Math.Round(rot / (Math.PI / 2)) * (Math.PI / 2);

        if (TryComp<PullableComponent>(ent, out var pullable) && pullable.Puller != null)
        {
            _pulling.TryStopPull(ent, pullable);
        }

        // TODO: Anchoring snaps rn anyway!
        if (ent.Comp.Snap)
        {
            var coordinates = xform.Coordinates.SnapToGrid(EntityManager);

            if (AnyUnstackable(ent, coordinates))
            {
                _popup.PopupEntity(Loc.GetString("construction-step-condition-no-unstackable-in-tile"), ent, user);
                return;
            }

            _transformSystem.SetCoordinates(ent, coordinates);
        }

        RaiseLocalEvent(ent, new BeforeAnchoredEvent(user, used));

        if (!xform.Anchored)
            _transformSystem.AnchorEntity(ent, xform);

        RaiseLocalEvent(ent, new UserAnchoredEvent(user, used));

        _popup.PopupEntity(Loc.GetString("anchorable-anchored"), ent, user);

        _adminLogger.Add(
            LogType.Anchor,
            LogImpact.Low,
            $"{ToPrettyString(user):user} anchored {ToPrettyString(ent):anchored} using {ToPrettyString(used):using}"
        );
    }

    /// <summary>
    ///     Tries to toggle the anchored status of this component's owner.
    ///     override is used due to popup and adminlog being server side systems in this case.
    /// </summary>
    /// <returns>true if toggled, false otherwise</returns>
    public void TryToggleAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
        AnchorableComponent? anchorable = null,
        TransformComponent? transform = null,
        PullableComponent? pullable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref transform))
            return;

        if (transform.Anchored)
        {
            TryUnAnchor(uid, userUid, usingUid, anchorable, transform, usingTool);
        }
        else
        {
            TryAnchor(uid, userUid, usingUid, anchorable, transform, pullable, usingTool);
        }
    }

    /// <summary>
    ///     Tries to anchor the entity.
    /// </summary>
    /// <returns>true if anchored, false otherwise</returns>
    private void TryAnchor(EntityUid uid, EntityUid userUid, EntityUid usingUid,
            AnchorableComponent? anchorable = null,
            TransformComponent? transform = null,
            PullableComponent? pullable = null,
            ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable, ref transform))
            return;

        // Optional resolves.
        Resolve(uid, ref pullable, false);

        if (!Resolve(usingUid, ref usingTool))
            return;

        if (!Valid(uid, userUid, usingUid, true, anchorable, usingTool))
            return;

        // Log anchor attempt (server only)
        _adminLogger.Add(LogType.Anchor, LogImpact.Low, $"{ToPrettyString(userUid):user} is trying to anchor {ToPrettyString(uid):entity} to {transform.Coordinates:targetlocation}");

        if (TryComp<PhysicsComponent>(uid, out var anchorBody) &&
            !TileFree(transform.Coordinates, anchorBody))
        {
            _popup.PopupEntity(Loc.GetString("anchorable-occupied"), uid, userUid);
            return;
        }

        if (AnyUnstackable(uid, transform.Coordinates))
        {
            _popup.PopupEntity(Loc.GetString("construction-step-condition-no-unstackable-in-tile"), uid, userUid);
            return;
        }

        _tool.UseTool(usingUid, userUid, uid, anchorable.Delay, usingTool.Qualities, new TryAnchorCompletedEvent());
    }

    private bool Valid(
        EntityUid uid,
        EntityUid userUid,
        EntityUid usingUid,
        bool anchoring,
        AnchorableComponent? anchorable = null,
        ToolComponent? usingTool = null)
    {
        if (!Resolve(uid, ref anchorable))
            return false;

        if (!Resolve(usingUid, ref usingTool))
            return false;

        if (anchoring && (anchorable.Flags & AnchorableFlags.Anchorable) == 0x0)
            return false;

        if (!anchoring && (anchorable.Flags & AnchorableFlags.Unanchorable) == 0x0)
            return false;

        BaseAnchoredAttemptEvent attempt =
            anchoring ? new AnchorAttemptEvent(userUid, usingUid) : new UnanchorAttemptEvent(userUid, usingUid);

        // Need to cast the event or it will be raised as BaseAnchoredAttemptEvent.
        if (anchoring)
            RaiseLocalEvent(uid, (AnchorAttemptEvent)attempt);
        else
            RaiseLocalEvent(uid, (UnanchorAttemptEvent)attempt);

        anchorable.Delay += attempt.Delay;

        return !attempt.Cancelled;
    }

    /// <summary>
    /// Returns true if no hard anchored entities exist on the coordinate tile that would collide with the provided physics body.
    /// </summary>
    public bool TileFree(EntityCoordinates coordinates, PhysicsComponent anchorBody)
    {
        // Probably ignore CanCollide on the anchoring body?
        var gridUid = _transformSystem.GetGrid(coordinates);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var tileIndices = _map.TileIndicesFor((gridUid.Value, grid), coordinates);
        return TileFree((gridUid.Value, grid), tileIndices, anchorBody.CollisionLayer, anchorBody.CollisionMask);
    }

    /// <summary>
    /// Returns true if no hard anchored entities match the collision layer or mask specified.
    /// </summary>
    /// <param name="grid"></param>
    public bool TileFree(Entity<MapGridComponent> grid, Vector2i gridIndices, int collisionLayer = 0, int collisionMask = 0)
    {
        var enumerator = _map.GetAnchoredEntitiesEnumerator(grid, grid.Comp, gridIndices);

        while (enumerator.MoveNext(out var ent))
        {
            if (!_physicsQuery.TryGetComponent(ent, out var body) ||
                !body.CanCollide ||
                !body.Hard)
            {
                continue;
            }

            if ((body.CollisionMask & collisionLayer) != 0x0 ||
                (body.CollisionLayer & collisionMask) != 0x0)
            {
                return false;
            }
        }

        return true;
    }

    [Obsolete("Use the Entity<MapGridComponent> version")]
    public bool TileFree(MapGridComponent grid, Vector2i gridIndices, int collisionLayer = 0, int collisionMask = 0)
    {
        return TileFree((grid.Owner, grid), gridIndices, collisionLayer, collisionMask);
    }

    /// <summary>
    /// Returns true if any unstackables are also on the corresponding tile.
    /// </summary>
    public bool AnyUnstackable(EntityUid uid, EntityCoordinates location)
    {
        DebugTools.Assert(!Transform(uid).Anchored);

        // If we are unstackable, iterate through any other entities anchored on the current square
        return _tagSystem.HasTag(uid, Unstackable) && AnyUnstackablesAnchoredAt(location);
    }

    public bool AnyUnstackablesAnchoredAt(EntityCoordinates location)
    {
        var gridUid = _transformSystem.GetGrid(location);

        if (!TryComp<MapGridComponent>(gridUid, out var grid))
            return false;

        var enumerator = _map.GetAnchoredEntitiesEnumerator(gridUid.Value, grid, _map.LocalToTile(gridUid.Value, grid, location));

        while (enumerator.MoveNext(out var entity))
        {
            // If we find another unstackable here, return true.
            if (_tagSystem.HasTag(entity.Value, Unstackable))
                return true;
        }

        return false;
    }

    [Serializable, NetSerializable]
    private sealed partial class TryUnanchorCompletedEvent : SimpleDoAfterEvent
    {
    }

    [Serializable, NetSerializable]
    private sealed partial class TryAnchorCompletedEvent : SimpleDoAfterEvent
    {
    }
}

[Serializable, NetSerializable]
public enum AnchorVisuals : byte
{
    Anchored
}
