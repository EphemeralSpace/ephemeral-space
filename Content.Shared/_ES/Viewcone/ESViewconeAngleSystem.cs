using System.Numerics;
using Content.Shared._ES.Viewcone.Components;
using Content.Shared.Examine;
using Content.Shared.Inventory;
using Content.Shared.StatusEffectNew;
using JetBrains.Annotations;
using Robust.Shared.Map;

namespace Content.Shared._ES.Viewcone;

/// <summary>
///     Public API for getting the actual modified viewcone angle (including equipment etc) rather than just the base angle
/// </summary>
public sealed partial class ESViewconeAngleSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESViewconeModifierComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESViewconeModifierComponent, ESViewconeGetAngleModifierEvent>(OnAngleModify);
        SubscribeLocalEvent<ESViewconeModifierComponent, InventoryRelayedEvent<ESViewconeGetAngleModifierEvent>>(OnAngleInventoryModify);
        SubscribeLocalEvent<ESViewconeModifierComponent, StatusEffectRelayedEvent<ESViewconeGetAngleModifierEvent>>(OnAngleStatusEffectModify);
    }

    private void OnExamined(Entity<ESViewconeModifierComponent> ent, ref ExaminedEvent args)
    {
        var loc = "es-viewcone-modifier-examine-increase";
        if (ent.Comp.AngleModifier < 0)
            loc = "es-viewcone-modifier-examine-decrease";

        var degrees = (int) MathF.Abs(ent.Comp.AngleModifier);
        args.PushMarkup(Loc.GetString(loc, ("degrees", degrees)));
    }

    private void OnAngleModify(Entity<ESViewconeModifierComponent> ent, ref ESViewconeGetAngleModifierEvent args)
    {
        args.ModifyAngle(ent.Comp.AngleModifier);
    }

    private void OnAngleInventoryModify(Entity<ESViewconeModifierComponent> ent, ref InventoryRelayedEvent<ESViewconeGetAngleModifierEvent> args)
    {
        args.Args.ModifyAngle(ent.Comp.AngleModifier);
    }

    private void OnAngleStatusEffectModify(Entity<ESViewconeModifierComponent> ent, ref StatusEffectRelayedEvent<ESViewconeGetAngleModifierEvent> args)
    {
        args.Args.ModifyAngle(ent.Comp.AngleModifier);
    }

    /// <summary>
    ///     Returns the modified viewcone angle for an entity, calculated from the base, taking into account
    ///     equipment & status effects & whatnot
    /// </summary>
    public float GetModifiedViewconeAngle(Entity<ESViewconeComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0f;

        var ev = new ESViewconeGetAngleModifierEvent();
        RaiseLocalEvent(ent, ref ev, true);

        // clamps to 0, 360 since this is additive and could easily go over with stacking equipment items and shit
        return Math.Clamp(ent.Comp.BaseConeAngle + ev.GetAngleModifier(), 0f, 360f);
    }

    /// <summary>
    ///     Checks if the target is inside an entity's viewcone.
    ///     This only cares about the actual cone on the screen. Something being visible technically is not covered here.
    ///     Additionally, this uses the viewer's entity rotation, which can be slightly desynced on the server.
    /// </summary>
    public bool InViewcone(Entity<ESViewconeComponent?> ent, EntityUid target)
    {
        var pos = _transform.GetWorldPosition(target);
        return InViewcone(ent, pos);
    }

    /// <summary>
    ///     Checks if a coordinate is inside an entity's viewcone.
    ///     This only cares about the actual cone on the screen. Something being visible technically is not covered here.
    ///     Additionally, this uses the viewer's entity rotation, which can be slightly desynced on the server.
    /// </summary>
    [PublicAPI]
    public bool InViewcone(Entity<ESViewconeComponent?> ent, EntityCoordinates coords)
    {
        var pos = _transform.ToWorldPosition(coords);
        return InViewcone(ent, pos);
    }

    /// <summary>
    ///     Checks if a coordinate is inside an entity's viewcone.
    ///     This only cares about the actual cone on the screen. Something being visible technically is not covered here.
    ///     Additionally, this uses the viewer's entity rotation, which can be slightly desynced on the server.
    /// </summary>
    /// <param name="ent">Entity whose viewcone is being checked</param>
    /// <param name="pos">World position being checked</param>
    public bool InViewcone(Entity<ESViewconeComponent?> ent, Vector2 pos)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return true; // 360 degree vision

        // TODO: Extracted literally from ESViewconeSetAlphaOverlay.Draw()
        // Could serve to be a lot simpler than it is.

        var (eyePos, eyeRot) = _transform.GetWorldPositionRotation(ent.Owner);

        var radConeAngle = MathHelper.DegreesToRadians(GetModifiedViewconeAngle(ent));
        var radConeFeather = MathHelper.DegreesToRadians(ent.Comp.ConeFeather);

        var dist = pos - eyePos;
        var distLength = dist.Length();
        var angleDist = Angle.ShortestDistance(dist.ToWorldAngle(), eyeRot);

        var angleAlpha = (float) Math.Clamp((Math.Abs(angleDist.Theta) - (radConeAngle * 0.5f)) + (radConeFeather * 0.5f), 0f, radConeFeather) / radConeFeather;
        var distAlpha = Math.Clamp((distLength - ent.Comp.ConeIgnoreRadius) + (ent.Comp.ConeIgnoreFeather * 0.5f), 0f, ent.Comp.ConeIgnoreFeather) / ent.Comp.ConeIgnoreFeather;
        var targetAlpha = Math.Max(1f - angleAlpha, 1f - distAlpha);

        // This leans on permissiveness and waves half-visible people as outside the viewcone.
        return MathHelper.CloseTo(targetAlpha, 1);
    }
}
