using Content.Shared.Damage.Components;
using Content.Shared.Damage.Events;
using Content.Shared.StatusEffectNew;

namespace Content.Shared.Damage.Systems;

public partial class SharedStaminaSystem
{
    private void InitializeModifier()
    {
        SubscribeLocalEvent<StaminaModifierStatusEffectComponent, StatusEffectAppliedEvent>(OnEffectApplied);
        SubscribeLocalEvent<StaminaModifierStatusEffectComponent, StatusEffectRemovedEvent>(OnEffectRemoved);
        SubscribeLocalEvent<StaminaModifierStatusEffectComponent, StatusEffectRelayedEvent<RefreshStaminaCritThresholdEvent>>(OnRefreshCritThreshold);
    }

    private void OnEffectApplied(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectAppliedEvent args)
    {
        RefreshStaminaCritThreshold(args.Target);
    }

    private void OnEffectRemoved(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        RefreshStaminaCritThreshold(args.Target);
    }

    private void OnRefreshCritThreshold(Entity<StaminaModifierStatusEffectComponent> ent, ref StatusEffectRelayedEvent<RefreshStaminaCritThresholdEvent> args)
    {
        var evArgs = args.Args;
// ES START
        var oldMod = evArgs.Modifier ?? -1;
        evArgs.Modifier = Math.Max(ent.Comp.Modifier, oldMod);
// ES END
        args.Args = evArgs;
    }

    public void RefreshStaminaCritThreshold(Entity<StaminaComponent?> entity)
    {
        if (!Resolve(entity, ref entity.Comp))
            return;

        var ev = new RefreshStaminaCritThresholdEvent(entity.Comp.BaseCritThreshold);
        RaiseLocalEvent(entity, ref ev);

// ES START
        ev.Modifier ??= 1; // default to 1 if there's no modified.
        entity.Comp.CritThreshold = ev.ThresholdValue * ev.Modifier.Value;
        Dirty(entity);
// ES END
    }
}
