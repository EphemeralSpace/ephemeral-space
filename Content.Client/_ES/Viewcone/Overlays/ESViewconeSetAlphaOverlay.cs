using Content.Client._ES.Viewcone.ComponentTree;
using Content.Client.Eye;
using Content.Shared._ES.Viewcone;
using Content.Shared._ES.Viewcone.Components;
using Content.Shared.MouseRotator;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Shared.Enums;
using Robust.Shared.Map.Components;

namespace Content.Client._ES.Viewcone.Overlays;

/// <summary>
///     Queries the bounds for each viewport for all <see cref="ESViewconeOccludableComponent"/>, then
///     sets their alpha before entities render in accordance with whether they should be in view or not
///
///     This alpha pass only works because of <see cref="ESViewconeResetAlphaOverlay"/>, which resets in a later stage of rendering.
/// </summary>
public sealed class ESViewconeSetAlphaOverlay : Overlay
{
    [Dependency] private readonly IEntityManager _ent = default!;
    private readonly ESViewconeOverlayManagementSystem _cone;
    private readonly ESViewconeAngleSystem _angle;
    private readonly ESViewconeOccludableTreeSystem _tree;
    private readonly TransformSystem _xform;
    private readonly SpriteSystem _sprite;

    public override OverlaySpace Space => OverlaySpace.WorldSpaceBelowEntities;

    // slightly sus but cached from beforedraw to use in draw.
    private Entity<EyeComponent, ESViewconeComponent>? _nextEye;

    public ESViewconeSetAlphaOverlay()
    {
        IoCManager.InjectDependencies(this);

        _cone = _ent.EntitySysManager.GetEntitySystem<ESViewconeOverlayManagementSystem>();
        _angle = _ent.EntitySysManager.GetEntitySystem<ESViewconeAngleSystem>();
        _tree = _ent.EntitySysManager.GetEntitySystem<ESViewconeOccludableTreeSystem>();
        _xform  = _ent.EntitySysManager.GetEntitySystem<TransformSystem>();
        _sprite = _ent.EntitySysManager.GetEntitySystem<SpriteSystem>();
    }

    protected override bool BeforeDraw(in OverlayDrawArgs args)
    {
        _nextEye = null;

        if (args.Viewport.Eye == null)
            return false;

        // This is really stupid but there isn't another way to reverse an eye entity from just an IEye afaict
        // It's not really inefficient though. theres only 1 of these anyway usually with the lerpingeye bound
        var enumerator = _ent.AllEntityQueryEnumerator<LerpingEyeComponent, EyeComponent, ESViewconeComponent>();
        while (enumerator.MoveNext(out var uid, out _, out var eye, out var viewcone))
        {
            if (args.Viewport.Eye != eye.Eye)
                continue;

            _nextEye = (uid, eye, viewcone);
            break;
        }

        return _nextEye != null;
    }

    protected override void Draw(in OverlayDrawArgs args)
    {
        if (_nextEye == null)
            return;

        var (ent, eye, cone) = _nextEye.Value;

        var eyeTransform = _ent.GetComponent<TransformComponent>(ent);
        var eyePos = _xform.GetWorldPosition(eyeTransform);
        var eyeRot = cone.ViewAngle - eye.Rotation; // subtract rotation cuz idk. the lerp adds it but this doesnt want it for some reason idk.

        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        // !! Thank You Bhijn God (TYBG) for 95% of the rest of this methods code !!
        // !!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!!
        var radConeAngle = MathHelper.DegreesToRadians(_angle.GetModifiedViewconeAngle((ent, cone)));
        var radConeFeather = MathHelper.DegreesToRadians(cone.ConeFeather);

        _cone.CachedBaseAlphas.Clear();
        var occludables = _tree.QueryAabb(args.MapId, args.WorldBounds);
        foreach (var entry in occludables)
        {
            var (comp, xform) = entry;
            var uid = entry.Uid; // this uses component.Owner.. oh well

            // dynamic clientside disabling, for effects like pulled entities
            if (_ent.HasComponent<ESViewconeClientNoOccludeComponent>(uid))
                continue;

            if (!_ent.TryGetComponent<SpriteComponent>(uid, out var sprite))
                continue;

            if (comp.Source == ent)
                continue;

            if (!comp.OccludeIfAnchored && xform.Anchored)
                continue;

            var entPos = _xform.GetWorldPosition(xform);

            var dist = entPos - eyePos;
            var distLength = dist.Length();
            var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot);

            var baseAlpha = sprite.Color.A;
            var angleAlpha = (float) Math.Clamp((Math.Abs(angleDist.Theta) - (radConeAngle * 0.5f)) + (radConeFeather * 0.5f), 0f, radConeFeather) / radConeFeather;
            var distAlpha = Math.Clamp((distLength - cone.ConeIgnoreRadius) + (cone.ConeIgnoreFeather * 0.5f), 0f, cone.ConeIgnoreFeather) / cone.ConeIgnoreFeather;
            var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

            // save the results so we can use it in resetalpha overlay
            _cone.CachedBaseAlphas.Add(((uid, sprite), baseAlpha));

            // multiply by the base alpha of the sprite (sprites which were already invisible for other reasons should stay invisible)
            var alpha = (comp.Inverted ? 1f - targetAlpha : targetAlpha) * (comp.OverrideBaseAlpha ? 1f : baseAlpha);
            _sprite.SetColor((uid, sprite), sprite.Color.WithAlpha(alpha));
            _sprite.SetVisible((uid, sprite), alpha > 0f);
        }
    }
}
