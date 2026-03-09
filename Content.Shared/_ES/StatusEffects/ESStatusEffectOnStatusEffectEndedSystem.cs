using Content.Shared._ES.StatusEffects.Components;
using Content.Shared.Chat;
using Content.Shared.Emoting;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._ES.StatusEffects;

public sealed class ESStatusEffectOnStatusEffectEndedSystem : EntitySystem
{
    [Dependency] private readonly StatusEffectsSystem _status = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESStatusEffectOnStatusEffectEndedComponent, StatusEffectRemovedEvent>(OnStatusEffectRemoved);
    }

    private void OnStatusEffectRemoved(Entity<ESStatusEffectOnStatusEffectEndedComponent> ent, ref StatusEffectRemovedEvent args)
    {
        _status.TryAddStatusEffectDuration(args.Target, ent.Comp.StatusEffect, ent.Comp.Duration, ent.Comp.Delay);
    }
}
