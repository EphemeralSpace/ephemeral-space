using Content.Server._ES.SpawnRegion.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map;

namespace Content.Server._ES.SpawnRegion;

public sealed class ESSpawnRandomRule : GameRuleSystem<ESSpawnRandomRuleComponent>
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly ESSpawnRegionSystem _spawnRegion = default!;

    protected override void Started(EntityUid uid,
        ESSpawnRandomRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var station))
            return;

        foreach (var spawn in _entityTable.GetSpawns(component.Table))
        {
            _ = component.SpawnRegion != null
                ? _spawnRegion.TryGetRandomCoordsInRegion(component.SpawnRegion.Value,
                    station.Value,
                    out var coords,
                    checkPlayerLOS: component.CheckPlayerLOS,
                    minPlayerDistance: component.MinPlayerDistance)
                : _spawnRegion.TryGetRandomCoords(station.Value,
                    out coords,
                    checkPlayerLOS: component.CheckPlayerLOS,
                    minPlayerDistance: component.MinPlayerDistance);

            if (coords == null)
                continue;

            SpawnAtPosition(spawn, coords.Value);
        }
    }
}
