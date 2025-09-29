using Content.Server._ES.Masks.Traitor.Components;
using Content.Server._ES.SpawnRegion;
using Content.Shared._ES.Auditions.Components;
using Content.Shared.EntityTable;
using Content.Shared.Maps;
using Robust.Server.GameObjects;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;

namespace Content.Server._ES.Masks.Traitor;

public sealed class ESMaskCacheSystem : EntitySystem
{
    [Dependency] private readonly ITileDefinitionManager _tileDefinition = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly ESSpawnRegionSystem _spawnRegion = default!;

    private EntityQuery<MapGridComponent> _gridQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMaskCacheSpawnerComponent, MapInitEvent>(OnMapInit);

        _gridQuery = GetEntityQuery<MapGridComponent>();
    }

    private void OnMapInit(Entity<ESMaskCacheSpawnerComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<ESCharacterComponent>(ent, out var character))
            return;

        if (!_spawnRegion.TryGetRandomAreaCoords(ent.Comp.Region, character.Station, out var coords, pred: CheckTileNotSubfloor))
        {
            Log.Debug("Failed to find spawn region!");
            return;
        }

        var spawns = _entityTable.GetSpawns(ent.Comp.CacheProto);
        foreach (var spawn in spawns)
        {
            SpawnAtPosition(spawn, coords.Value);
        }
    }

    private bool CheckTileNotSubfloor(Entity<TransformComponent> ent)
    {
        // Gonna love resolving this over and over
        if (!_gridQuery.TryComp(ent.Comp.GridUid, out var mapGrid))
            return false;

        if (!_map.TryGetTileRef(ent.Comp.GridUid.Value, mapGrid, ent.Comp.Coordinates, out var tileRef))
            return false;
        var tile = (ContentTileDefinition)_tileDefinition[tileRef.Tile.TypeId];
        return !tile.IsSubFloor;
    }
}
