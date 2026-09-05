using System.Numerics;
using Content.Client._ES.Chat;
using Content.Client.Examine;
using Content.Shared._ES.Auditions;
using Content.Shared.Humanoid;
using Content.Shared.IdentityManagement;
using Content.Shared.Interaction;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Shared.Enums;
using Robust.Shared.Light;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.NamePeek;

/// <summary>
/// Handles the name peek overlay.
/// Overlay will show names underneath mob entities when Visible is true in NamePeekSystem
/// </summary>
public sealed partial class NamePeekOverlay : Overlay
{
    private static readonly ProtoId<ShaderPrototype> UnshadedShader = "unshaded";

    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IPlayerManager _playerManager = default!;
    [Dependency] private IInputManager _inputManager = default!;
    [Dependency] private ILocalizationManager _loc = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;

    private readonly ExamineSystem _examineSystem;
    private readonly EntityLookupSystem _lookup;
    private readonly ESNamePeekSystem _namePeekSystem;
    private readonly ESChatSystem _chat;
    private readonly LightLevelSystem _lightLevel;
    private readonly ShaderInstance _shader;
    private readonly SharedTransformSystem _transform;
    private readonly SpriteSystem _sprite;

    private EntityQuery<SpriteComponent> _spriteQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MobStateComponent> _mobStateQuery;
    private EntityQuery<HumanoidProfileComponent> _humanoidProfileQuery;

    private readonly HashSet<Entity<MobStateComponent>> _nearbyEntities = new();

    private readonly Font _font;
    private readonly Font _smallFont;

    //Maybe change to WorldSpace if DrawString gets added to WorldHandle for lighting on tag
    public override OverlaySpace Space => OverlaySpace.ScreenSpace;

    public NamePeekOverlay(
        ExamineSystem examine,
        EntityLookupSystem lookup,
        ESNamePeekSystem namePeek,
        ESChatSystem chat,
        LightLevelSystem lightLevel,
        SharedTransformSystem transform,
        SpriteSystem sprite,
        EntityQuery<SpriteComponent> spriteQuery,
        EntityQuery<TransformComponent> transformQuery,
        EntityQuery<MobStateComponent> mobStateQuery,
        EntityQuery<HumanoidProfileComponent> humanoidProfileQuery)
    {
        _examineSystem = examine;
        _lookup = lookup;
        _namePeekSystem = namePeek;
        _chat = chat;
        _lightLevel = lightLevel;
        _transform = transform;
        _sprite = sprite;

        _spriteQuery = spriteQuery;
        _transformQuery = transformQuery;
        _mobStateQuery = mobStateQuery;
        _humanoidProfileQuery = humanoidProfileQuery;

        IoCManager.InjectDependencies(this);

        _shader = _prototypeManager.Index(UnshadedShader).Instance();
        var cache = IoCManager.Resolve<IResourceCache>();
        _font = new VectorFont(cache.GetResource<FontResource>("/Fonts/_ES/Wormtown9k-Regular.ttf"), 12);
        _smallFont = new VectorFont(cache.GetResource<FontResource>("/Fonts/_ES/Wormtown9k-Small.ttf"), 12);
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null || !_namePeekSystem.Visible)
            return false;

        //Don't draw names if we're crit
        if (_mobStateQuery.TryComp(_playerManager.LocalEntity, out var mobState)
            && (mobState.CurrentState == MobState.Critical))
            return false;

        return true;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (args.ViewportControl == null)
            return;

        if (_playerManager.LocalEntity is not { } playerEnt)
            return;

        if (args.Viewport.Eye is not { } eye)
            return;

        args.DrawingHandle.SetTransform(Matrix3x2.Identity);
        args.DrawingHandle.UseShader(_shader);

        var scale = (args.ViewportControl as Control)?.UIScale ?? 0f;

        if (scale <= 0f)
            return;

        var handle = args.ScreenHandle;
        var matrix = args.ViewportControl.GetWorldToScreenMatrix();

        _nearbyEntities.Clear();

        if (eye.DrawFov)
        {
            //Lookup near mouse when we have FOV on
            var mousePos = args.ViewportControl.PixelToMap(_inputManager.MouseScreenPosition.Position);
            _lookup.GetEntitiesInRange(mousePos, SharedInteractionSystem.InteractionRange / 2, _nearbyEntities, LookupFlags.Uncontained);
        }
        else
        {
            _lookup.GetEntitiesIntersecting(args.MapId, args.WorldAABB, _nearbyEntities, LookupFlags.Uncontained);
        }

        foreach (var ent in _nearbyEntities)
        {
            if (ent.Owner == playerEnt)
                continue;

            if (!_transformQuery.TryComp(ent, out var xform))
                continue;

            if (!_spriteQuery.TryComp(ent, out var sprite) || !sprite.Visible || sprite.Color.A <= 0f)
                continue;

            var mapPos = _transform.GetMapCoordinates((ent, xform));

            var lightLevel = 1f;
            if (eye.DrawLight && eye.DrawFov)
                _lightLevel.TryCalculateLightLevel(mapPos, out lightLevel);

            //Don't show nametag if it's too dark
            //Most maintenance tunnels on toast seem to be below 0.9, main halls are usually 1
            if (lightLevel <= 0.7)
                continue;

            if (eye.DrawFov && !_examineSystem.InRangeUnOccluded(playerEnt, ent))
                continue;

            var text = Identity.Name(ent, _entityManager, playerEnt);

            //Text dimensions for centering
            var dimensions = handle.GetDimensions(_font, text, scale);

            //Get sprite bounding box so we can draw at the bottom.
            //Probably a better way to do this, but I want it drawing at the bottom of entity sprites if possible.
            var (worldPos, worldRot) = _transform.GetWorldPositionRotation(xform);
            var bounds = _sprite.CalculateBounds((ent, sprite),
                 worldPos,
                 worldRot,
                 eye.Rotation);

            var offsetBounds = bounds.Box.Enlarged(0.15f);
            var offset = (-eye.Rotation).ToWorldVec() * (-offsetBounds.Extents.Y);
            var offsetWorldPos = worldPos - offset;

            var pos = Vector2.Transform(offsetWorldPos, matrix);
            var drawPosition = (pos - dimensions / 2f);
            var color = _chat.GetChatColor(text);
            var outlineColor = _chat.GetChatOutlineColor(color);
            var outline = new TextOutline(2f, outlineColor);

            handle.DrawString(_font, drawPosition, text, scale, color, outline);

            if (_humanoidProfileQuery.TryGetComponent(ent, out var humanoid))
            {
                var pronouns = humanoid.Gender.GetPronounString(_loc);

                var pronounDimensions = handle.GetDimensions(_smallFont, pronouns, scale);
                var pronounsDrawPosition = pos - pronounDimensions / 2f;
                pronounsDrawPosition.Y += dimensions.Y;

                handle.DrawString(_smallFont, pronounsDrawPosition, pronouns, scale, color, outline);
            }
        }

        args.DrawingHandle.UseShader(null);
    }
}
