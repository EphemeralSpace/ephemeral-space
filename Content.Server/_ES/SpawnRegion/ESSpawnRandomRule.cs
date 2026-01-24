using Content.Server._ES.SpawnRegion.Components;
using Content.Server.GameTicking.Rules;
using Content.Shared.EntityTable;
using Content.Shared.GameTicking.Components;

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
            if (!_spawnRegion.TryGetRandomCoords(station.Value, out var coords, checkPlayerLOS: false, minPlayerDistance: 5f))
                continue;

            SpawnAtPosition(spawn, coords.Value);
        }
    }
}
