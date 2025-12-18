using Content.Shared.Stealth;
using Content.Shared.Stealth.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Physics.Events;

namespace Content.Shared._ES.Stealth;

public sealed class StealthOnCollideSystem : EntitySystem
{
    [Dependency] private readonly SharedStealthSystem _stealth = default!;
    [Dependency] private readonly EntityWhitelistSystem _whitelist = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<StealthOnCollideComponent, StartCollideEvent>(OnCollide);
        SubscribeLocalEvent<StealthOnCollideComponent, SharedStealthSystem.GetVisibilityModifiersEvent>(OnGetVisibilityModifiers);
    }

    private void OnCollide(EntityUid uid, StealthOnCollideComponent component, ref StartCollideEvent args)
    {
        if (!HasComp<StealthComponent>(uid))
            return;

        if (!_whitelist.IsWhitelistPass(component.Whitelist, args.OtherEntity))
            return;

        _stealth.ModifyVisibility(uid, component.StealthToChange);
    }

    private void OnGetVisibilityModifiers(EntityUid uid, StealthOnCollideComponent component, SharedStealthSystem.GetVisibilityModifiersEvent args)
    {
        var mod = args.SecondsSinceUpdate * component.PassiveVisibilityRate;
        args.FlatModifier += mod;
    }
}
