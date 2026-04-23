using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Mind;
using Content.Shared._ES.TileFires;
using Robust.Shared.Map;

namespace Content.Server._ES.Masks.Objectives.Relays;

public sealed class ESTileFireRelaySystem : ESBaseMindRelay
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTileFireRelayComponent, ESTileFireCreatedEvent>(OnTileFireCreated);
    }

    private void OnTileFireCreated(Entity<ESTileFireRelayComponent> ent, ref ESTileFireCreatedEvent args)
    {
        if (!TryGetMind(ent, out var mind))
            return;

        var ev = new ESBodyCreatedTileFireEvent(args.Coordinates, ent, args.Stage);
        RaiseMindEvent(mind.Value, ref ev);
    }
}

[ByRefEvent]
public readonly record struct ESBodyCreatedTileFireEvent(EntityCoordinates Coordinates, EntityUid User, int Stage = 1);
