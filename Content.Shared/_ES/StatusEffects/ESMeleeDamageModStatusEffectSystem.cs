using System.Linq;
using Content.Shared._ES.StatusEffects.Components;
using Content.Shared.Damage;
using Content.Shared.Damage.Prototypes;
using Content.Shared.StatusEffectNew;
using Content.Shared.Weapons.Melee.Events;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.StatusEffects;

public sealed class ESMeleeDamageModStatusEffectSystem : EntitySystem
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMeleeDamageModStatusEffectComponent, StatusEffectRelayedEvent<GetMeleeDamageEvent>>(OnGetMeleeDamage);
    }

    private void OnGetMeleeDamage(Entity<ESMeleeDamageModStatusEffectComponent> ent, ref StatusEffectRelayedEvent<GetMeleeDamageEvent> args)
    {
        var coefficients = _prototype.EnumeratePrototypes<DamageTypePrototype>()
            .Select(p => (p.ID, ent.Comp.Coefficient))
            .ToDictionary();

        args.Args.Modifiers.Add(new DamageModifierSet
        {
            Coefficients = coefficients,
        });
    }
}
