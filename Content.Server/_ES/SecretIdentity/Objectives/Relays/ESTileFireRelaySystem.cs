using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.Mind;
using Content.Shared._ES.TileFires;
using Content.Shared.Atmos;
using Robust.Shared.Map;

namespace Content.Server._ES.SecretIdentity.Objectives.Relays;

public sealed class ESTileFireRelaySystem : ESBaseMindRelay
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTileFireRelayComponent, ESTileFireCreatedEvent>(OnTileFireCreated);
        SubscribeLocalEvent<ESTileFireRelayComponent, ESUserExtinguishedEvent>(OnUserExtinguished);
    }

    private void OnTileFireCreated(Entity<ESTileFireRelayComponent> ent, ref ESTileFireCreatedEvent args)
    {
        if (!TryGetMind(ent, out var mind))
            return;

        var ev = new ESBodyCreatedTileFireEvent(args.Coordinates, ent);
        RaiseMindEvent(mind.Value, ref ev);
    }

    private void OnUserExtinguished(Entity<ESTileFireRelayComponent> ent, ref ESUserExtinguishedEvent args)
    {
        if (!TryGetMind(ent, out var mind))
            return;

        if (!HasComp<ESTileFireComponent>(args.Flammable))
            return;

        var ev = new ESBodyExtinguishedTileFireEvent(args.Flammable, ent);
        RaiseMindEvent(mind.Value, ref ev);
    }
}

[ByRefEvent]
public readonly record struct ESBodyCreatedTileFireEvent(EntityCoordinates Coordinates, EntityUid User);

[ByRefEvent]
public readonly record struct ESBodyExtinguishedTileFireEvent(EntityUid Fire, EntityUid User);
