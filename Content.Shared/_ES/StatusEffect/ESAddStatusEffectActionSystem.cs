using Content.Shared.Actions;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.StatusEffect;

public sealed class ESAddStatusEffectActionSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<StatusEffectContainerComponent, ESAddStatusEffectActionEvent>(OnAddStatusEffectAction);
    }

    private void OnAddStatusEffectAction(Entity<StatusEffectContainerComponent> ent, ref ESAddStatusEffectActionEvent args)
    {
        args.Handled = _statusEffects.TryAddStatusEffectDuration(ent, args.Effect, out _, args.Duration);
    }
}

public sealed partial class ESAddStatusEffectActionEvent : InstantActionEvent
{
    [DataField(required: true)]
    public EntProtoId<StatusEffectComponent> Effect = default!;

    [DataField(required: true)]
    public TimeSpan Duration;
}
