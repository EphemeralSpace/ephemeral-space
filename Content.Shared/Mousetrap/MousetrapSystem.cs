using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Trigger.Systems;
using Content.Shared.StepTrigger.Systems;

namespace Content.Shared.Mousetrap;

public sealed class MousetrapSystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<MousetrapComponent, BeforeDamageOnTriggerEvent>(BeforeDamageOnTrigger);
        SubscribeLocalEvent<MousetrapComponent, StepTriggerAttemptEvent>(OnStepTriggerAttempt);
    }

    // only allow step triggers to trigger if the trap is armed
    // TODO: refactor Steptriggers to get rid of this
    // they should just use the new trigger conditions
    private void OnStepTriggerAttempt(Entity<MousetrapComponent> ent, ref StepTriggerAttemptEvent args)
    {
        if (!TryComp<ItemToggleComponent>(ent, out var toggle))
            return;

        args.Continue |= toggle.Activated;
    }

    private void BeforeDamageOnTrigger(Entity<MousetrapComponent> ent, ref BeforeDamageOnTriggerEvent args)
    {
        if (HasComp<ESMousetrapPestComponent>(args.Tripper))
        {
            args.Damage += ent.Comp.MouseDamage;
        }
    }
}
