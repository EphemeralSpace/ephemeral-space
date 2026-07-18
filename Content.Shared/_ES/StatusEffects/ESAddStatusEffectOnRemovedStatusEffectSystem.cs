using Content.Shared._ES.StatusEffects.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.StatusEffectNew.Components;

namespace Content.Shared._ES.StatusEffects;

public sealed partial class ESAddStatusEffectOnRemovedStatusEffectSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESAddStatusEffectOnRemovedStatusEffectComponent, StatusEffectRemovedEvent>(OnStatusEffectEnded);
    }

    private void OnStatusEffectEnded(Entity<ESAddStatusEffectOnRemovedStatusEffectComponent> ent, ref StatusEffectRemovedEvent args)
    {
        if (!TryComp<StatusEffectComponent>(ent, out var effect))
            return;

        if (effect.AppliedTo is not { } target)
            return;

        _status.TryAddStatusEffectDuration(target, ent.Comp.StatusEffect, ent.Comp.Duration);
    }
}
