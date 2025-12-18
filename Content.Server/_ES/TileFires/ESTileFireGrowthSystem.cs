using Content.Server.Atmos.Components;
using Content.Server.Atmos.EntitySystems;
using Content.Server.Spreader;
using Content.Shared._ES.TileFires;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Content.Shared.Random.Helpers;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._ES.TileFires;

/// <summary>
///     Server-side logic for tile fire growth logic, e.g. stages, requiring oxygen, etc.
///     See <see cref="ESTileFireSystem"/> for the shared API for actually spawning them.
/// </summary>
public sealed class ESTileFireGrowthSystem : EntitySystem
{
    [Dependency] private readonly AtmosphereSystem _atmos = default!;
    [Dependency] private readonly FlammableSystem _flammable = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly TurfSystem _turf = default!;
    [Dependency] private readonly IRobustRandom _random = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTileFireComponent, SpreadNeighborsEvent>(OnSpreadNeighbors);
    }

    private void OnSpreadNeighbors(Entity<ESTileFireComponent> ent, ref SpreadNeighborsEvent args)
    {
        if (!TryComp<FlammableComponent>(ent, out var flammable))
            return;

        if (!_random.Prob(ent.Comp.BaseSpreadChance))
            return;

        // random alteration to firestacks required for variance
        if (flammable.FireStacks < ent.Comp.MinFirestacksToSpread * _random.NextFloat(0.75f, 1.25f))
            return;

        if (args.NeighborFreeTiles.Count == 0)
        {
            RemCompDeferred<ActiveEdgeSpreaderComponent>(ent);
            return;
        }

        // Score neighboring tiles based on criteria, then do a weighted pick to spread
        Dictionary<EntityCoordinates, float> weights = new(args.NeighborFreeTiles.Count);
        foreach (var neighbor in args.NeighborFreeTiles)
        {
            // not updating the spreader api to get rid of this .owner sorry too many breakchanges for me
            var grid = neighbor.Grid.Owner;
            if (!TryComp<GridAtmosphereComponent>(grid, out var gridComp))
                continue;

            var tileDef = _turf.GetContentTileDefinition(neighbor.Tile);
            var score = tileDef.Flammability;

            // no atmosphere = definitely dont score this tile (shouldnt be possible anyway afaik)
            if (_atmos.GetTileMixture((grid, gridComp, null), null, neighbor.Tile.GridIndices) is not { } mixture)
                continue;

            if (mixture.Temperature > Atmospherics.FireMinimumTemperatureToSpread)
                score *= 4;
            else if (mixture.Temperature > Atmospherics.FireMinimumTemperatureToExist)
                score *= 2;

            // TODO ES fires dont actually fizzle out if theres no oxygen in the tile -after- they spread
            // TODO and they dont use oxygen either
            if (mixture.GetMoles(Gas.Oxygen) < ent.Comp.MinimumOxyMolesToSpread)
                score *= 0;
            if (_atmos.GetHeatCapacity(mixture, false) < Atmospherics.MinimumHeatCapacity)
                score *= 0;

            var coords = _map.GridTileToLocal(neighbor.Tile.GridUid, neighbor.Grid, neighbor.Tile.GridIndices);
            weights.Add(coords, score);
        }

        while (args.Updates > 0)
        {
            if (flammable.FireStacks < ent.Comp.MinFirestacksToSpread)
                return;

            var coords = _random.PickAndTake(weights);
            Spawn(ent.Comp.Prototype, coords);

            _flammable.AdjustFireStacks(ent, _random.NextFloat(0.25f, 1.25f) * -ent.Comp.FirestacksRemoveOnSpread, flammable);
            args.Updates--;
        }
    }
}
