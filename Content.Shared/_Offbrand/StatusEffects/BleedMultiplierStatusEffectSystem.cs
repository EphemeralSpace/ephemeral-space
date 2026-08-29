using Content.Shared._Offbrand.Wounds;
using Content.Shared.Body.Systems;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._Offbrand.StatusEffects;

public sealed partial class BleedMultiplierStatusEffectSystem : EntitySystem
{
    [Dependency] private SharedBloodstreamSystem _bloodstream = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<BleedMultiplierStatusEffectComponent, StatusEffectAppliedEvent>(OnApplied);
        SubscribeLocalEvent<BleedMultiplierStatusEffectComponent, StatusEffectRemovedEvent>(OnRemoved);
        SubscribeLocalEvent<BleedMultiplierStatusEffectComponent, StatusEffectRelayedEvent<ModifyBleedLevelEvent>>(OnGetBleedMultiplier);
    }

    private void OnApplied(Entity<BleedMultiplierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        _bloodstream.UpdateBleedAlert(args.Target);
    }

    private void OnRemoved(Entity<BleedMultiplierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _bloodstream.UpdateBleedAlert(args.Target);
    }

    private void OnGetBleedMultiplier(Entity<BleedMultiplierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<ModifyBleedLevelEvent> args)
    {
        args.Args = args.Args with { BleedLevel = args.Args.BleedLevel * ent.Comp.Multiplier };
    }
}
