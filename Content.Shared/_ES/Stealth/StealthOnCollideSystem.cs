using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Robust.Shared.Physics.Events;

namespace Content.Shared._ES.Stealth;

public sealed class StealthOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StealthOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<StealthOnCollideComponent, SharedStealthSystem.GetVisibilityModifiersEvent>(OnGetVisibilityModifiers);
    }

    private void OnCollide(EntityUid uid, StealthOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!HasComp<StealthComponent>(uid))
            return;

        var CurrentStealth = _stealth.GetVisibility(uid);

        var NewStealth = (CurrentStealth + component.StealthToChange);

        _stealth.SetVisibility(uid, NewStealth);
    }

    private void OnGetVisibilityModifiers(EntityUid uid, StealthOnCollideComponent component, SharedStealthSystem.GetVisibilityModifiersEvent args)
    {
        var mod = args.SecondsSinceUpdate * component.PassiveVisibilityRate;
        args.FlatModifier += mod;
    }
}
