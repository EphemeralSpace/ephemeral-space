using Content.Shared._ES.Fire.Components;
using Content.Shared.Atmos;
using Content.Shared.StatusEffectNew;

namespace Content.Shared._ES.Fire;

public sealed partial class ESPyroeuphoricSystem : EntitySystem
{
    [Dependency] private StatusEffectsSystem _statusEffects = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPyroeuphoricComponent, IgnitedEvent>(OnIgnited);
        SubscribeLocalEvent<ESPyroeuphoricComponent, ExtinguishedEvent>(OnExtinguished);
    }

    private void OnIgnited(Entity<ESPyroeuphoricComponent> ent, ref IgnitedEvent args)
    {
        Log.Debug("ignited!");
        _statusEffects.TrySetStatusEffectDuration(ent, ent.Comp.StatusEffect);
    }

    private void OnExtinguished(Entity<ESPyroeuphoricComponent> ent, ref ExtinguishedEvent args)
    {
        Log.Debug("extinguished!");
        _statusEffects.TryRemoveStatusEffect(ent, ent.Comp.StatusEffect);
    }
}
