using Content.Server._ES.Filth.Components;
using Content.Server._ES.SpawnRegion;
using Content.Server.Atmos.EntitySystems;
using Content.Server.GameTicking.Rules;
using Content.Server.Station.Systems;
using Content.Shared.Atmos;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ES.Filth;

public sealed partial class ESMiasmaGeneratorRule : GameRuleSystem<ESMiasmaGeneratorRuleComponent>
{
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private AtmosphereSystem _atmosphere = default!;
    [Dependency] private EntityTableSystem _entityTable = default!;
    [Dependency] private MapSystem _map = default!;
    [Dependency] private ESSpawnRegionSystem _spawnRegion = default!;
    [Dependency] private StationSystem _station = default!;

    protected override void Added(EntityUid uid,
        ESMiasmaGeneratorRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleAddedEvent args)
    {
        component.NextUpdate = _timing.CurTime + component.UpdateRate;
    }

    protected override void ActiveTick(EntityUid uid, ESMiasmaGeneratorRuleComponent component, GameRuleComponent gameRule, float frameTime)
    {
        if (_timing.CurTime < component.NextUpdate)
            return;
        component.NextUpdate += component.UpdateRate;

        foreach (var station in _station.GetStations())
        {
            var count = GetMiasmaEventCount((uid, component), station, out var validCoords);

            for (var i = 0; i < count; ++i)
            {
                if (!_spawnRegion.TryGetRandomCoords(
                        validCoords,
                        out var coords,
                        checkPlayerLOS: false))
                    break;

                foreach (var spawn in _entityTable.GetSpawns(component.SpawnTable))
                {
                    SpawnAtPosition(spawn, coords.Value);
                }
            }
        }
    }

    private int GetMiasmaEventCount(Entity<ESMiasmaGeneratorRuleComponent> ent, EntityUid station, out HashSet<EntityCoordinates> coords)
    {
        var count = 0;
        coords = new();

        foreach (var grid in _station.GetGrids(station))
        {
            if (!TryComp<MapGridComponent>(grid, out var gridComp))
                continue;

            foreach (var tile in _map.GetAllTiles(grid, gridComp))
            {
                if (_atmosphere.GetTileMixture(tile.GridUid, null, tile.GridIndices) is not { } mixture)
                    continue;

                if (mixture.GetMoles(Gas.Miasma) < ESMiasmaGeneratorRuleComponent.MinEventMols)
                    continue;
                count++;
                coords.Add(_map.GridTileToLocal(grid, gridComp, tile.GridIndices));
            }
        }

        var eventCount = (float) count / ent.Comp.TilesPerEvent;

        if (eventCount >= 1)
            return (int) Math.Round(eventCount);

        return _random.Prob(eventCount) ? 1 : 0;
    }
}
