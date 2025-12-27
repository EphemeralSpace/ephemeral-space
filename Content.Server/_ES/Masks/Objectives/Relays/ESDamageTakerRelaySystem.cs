using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Mind;
using Content.Shared._ES.Mind;
using Content.Shared.Damage;
using Content.Shared.Damage.Components;
using Content.Shared.Damage.Systems;

namespace Content.Server._ES.Masks.Objectives.Relays;

/// <summary>
///     This handles relaying <see cref="DamageChangedEvent"/> to the mind, allowing other objectives to listen to it.
/// </summary>
public sealed class ESDamageTakerRelaySystem : ESBaseMindRelay
{
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDamageTakerRelayComponent, DamageChangedEvent>(OnDamageChanged);
    }

    private void OnDamageChanged(Entity<ESDamageTakerRelayComponent> ent, ref DamageChangedEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        if (!HasComp<DamageableComponent>(ent))
            return;

        if (args.DamageDelta != null)
        {
            var ev = new ESDamageTakenEvent(ent, args.DamageIncreased, args.DamageDelta, args.Origin);

            RaiseMindEvent((mindId, mindComp), ref ev);
        }
    }
}

/// <summary>
///     Raised directed on the mind when the body has taken damage.
/// </summary>
/// <param name="Body">The body in question.</param>
/// <param name="DamageIncreased">Did damage increase or not?</param>
/// <param name="DamageDone">The amount of damage done</param>
/// <param name="Origin">The origin of damage</param>
[ByRefEvent]
public readonly record struct ESDamageTakenEvent(EntityUid Body, bool DamageIncreased, DamageSpecifier DamageDone, EntityUid? Origin);
