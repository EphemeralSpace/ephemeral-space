using System.Linq;
using System.Numerics;
using Content.Shared.CombatMode;
using Content.Shared.Decals;
using Content.Shared.Hands;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.IdentityManagement;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Crosshair;

public sealed partial class ESCrosshairSystem : EntitySystem
{
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedCombatModeSystem _combat = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private SharedTransformSystem _xform = default!;

    private const float LerpHalfLife = 0.02f;
    private static readonly EntProtoId CrosshairEffect = "ESCrosshairEffect";

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCrosshairProviderComponent, HandDeselectedEvent>(OnHandDeselected);
        SubscribeLocalEvent<ESCrosshairProviderComponent, HandSelectedEvent>(OnHandSelected);
        SubscribeLocalEvent<ESCrosshairAimerComponent, CombatModeToggledEvent>(OnCombatModeToggled);
        SubscribeLocalEvent<ESCrosshairAimerComponent, EntityTerminatingEvent>(OnAimerTerminating);

        SubscribeNetworkEvent<ESCrosshairNetworkEvent>(OnCrosshair);
    }

    private void OnHandDeselected(Entity<ESCrosshairProviderComponent> ent, ref HandDeselectedEvent args)
    {
        if (!TryComp<ESCrosshairAimerComponent>(args.User, out var provider))
            return;

        SetCrosshair((args.User, provider), false);
    }

    private void OnHandSelected(Entity<ESCrosshairProviderComponent> ent, ref HandSelectedEvent args)
    {
        if (!TryComp<ESCrosshairAimerComponent>(args.User, out var provider))
            return;

        if (!ent.Comp.RequiresCombatMode || _combat.IsInCombatMode(args.User))
        {
            SetCrosshair((args.User, provider), true);
        }
    }

    #region Events / API

    private void OnCombatModeToggled(Entity<ESCrosshairAimerComponent> ent, ref CombatModeToggledEvent args)
    {
        if (!_hands.TryGetActiveItem(ent.Owner, out var item)
            || !TryComp<ESCrosshairProviderComponent>(item, out var provider))
        {
            SetCrosshair(ent.AsNullable(), false);
            return;
        }

        var valid = args.Enabled || !provider.RequiresCombatMode;
        SetCrosshair(ent.AsNullable(), valid);
    }

    private void OnAimerTerminating(Entity<ESCrosshairAimerComponent> ent, ref EntityTerminatingEvent args)
    {
        var crosshair = ent.Comp.CrosshairEntity;
        if (crosshair is not null && !TerminatingOrDeleted(crosshair))
        {
            QueueDel(crosshair);
        }
    }

    [PublicAPI]
    public void SetCrosshair(Entity<ESCrosshairAimerComponent?> entity, bool enabled)
    {
        if (!Resolve(entity, ref entity.Comp, false))
            return;

        // todo this could probably get reused for npcs to instead point at whatever their target is somehow
        // but for now no
        if (!HasComp<ActorComponent>(entity))
            return;

        if (entity.Comp.CrosshairEntity is not null && enabled
            || entity.Comp.CrosshairEntity is null && !enabled)
            return;

        if (enabled)
        {
            entity.Comp.CrosshairEntity = PredictedSpawnAtPosition(CrosshairEffect, Transform(entity).Coordinates);
            var comp = new ESCrosshairEntityComponent() { User = entity.Owner };
            AddComp(entity.Comp.CrosshairEntity.Value, comp);
            _appearance.SetData(entity.Comp.CrosshairEntity.Value, ESCrosshairVisuals.Name, Identity.Name(entity.Owner, EntityManager));
        }
        else
        {
            PredictedQueueDel(entity.Comp.CrosshairEntity);
            entity.Comp.CrosshairEntity = null;
        }

        Dirty(entity);
    }

    #endregion

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESCrosshairEntityComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var entity, out var xform))
        {
            var target = entity.Target;
            if (target == MapCoordinates.Nullspace)
                continue;

            var coords = _xform.GetMapCoordinates(xform);
            var newCoordinates = new MapCoordinates(Vector2.Lerp(coords.Position,
                target.Position,
                1f - MathF.Pow(2f, -(frameTime / LerpHalfLife))),
                target.MapId);

            _xform.SetMapCoordinates((uid, xform), newCoordinates);
        }
    }

    private void OnCrosshair(ESCrosshairNetworkEvent msg, EntitySessionEventArgs args)
    {
        if (!msg.Coordinates.Position.IsValid())
            return;

        if (args.SenderSession.AttachedEntity is not { } ent || !TryComp<ESCrosshairAimerComponent>(ent, out var aimer))
            return;

        var crosshairEntity = aimer.CrosshairEntity;
        if (crosshairEntity is null || !TryComp<ESCrosshairEntityComponent>(crosshairEntity, out var crosshair)) // wait for it to get spawned
            return;

        var userXform = Transform(ent);
        if (userXform.MapID != msg.Coordinates.MapId)
            return;

        crosshair.Target = msg.Coordinates;
        Dirty(crosshairEntity.Value, crosshair);
    }
}
