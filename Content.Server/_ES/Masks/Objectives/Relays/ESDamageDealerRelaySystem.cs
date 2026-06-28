using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Mind;
using Content.Shared._ES.Mind;
using Content.Shared.Damage.Systems;

namespace Content.Server._ES.Masks.Objectives.Relays;

/// <summary>
///     This handles relaying <see cref="DamageChangedEvent"/> to the mind, allowing other objectives to listen to it.
/// </summary>
public sealed partial class ESDamageDealerRelaySystem : ESBaseMindRelay
{
    [Dependency] private MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDamageDealerRelayComponent, ESCausedDamageChanged>(OnCausedDamageChanged);
    }

    private void OnCausedDamageChanged(Entity<ESDamageDealerRelayComponent> ent, ref ESCausedDamageChanged args)
    {
        if (!_mind.TryGetMind(ent, out var mind))
            return;

        RaiseMindEvent(mind.Value, ref args);
    }
}
