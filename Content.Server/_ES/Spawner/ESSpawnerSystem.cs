using Content.Server._ES.Spawner.Components;
using Content.Shared._ES.Core.Entity;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Spawner;

/// <summary>
/// Handles custom spawners for Ephemeral Space
/// </summary>
public sealed class ESSpawnerSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    private readonly HashSet<Entity<ESDistributedSpawnerMarkerComponent>> _markers = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDistributedSpawnerComponent, MapInitEvent>(OnDistributedMapInit);
    }

    private void OnDistributedMapInit(Entity<ESDistributedSpawnerComponent> ent, ref MapInitEvent args)
    {
        Spawn(ent);
        if (ent.Comp.DeleteSpawnerAfterSpawn && !TerminatingOrDeleted(ent) && Exists(ent))
            QueueDel(ent);
    }

    private void Spawn(Entity<ESDistributedSpawnerComponent> ent)
    {
        if (TerminatingOrDeleted(ent) || !Exists(ent))
            return;
        if (ent.Comp.Tables.Count == 0)
            return;

        var xform = Transform(ent);

        _markers.Clear();
        _entityLookup.GetEntitiesInRange(xform.Coordinates, ent.Comp.Range, _markers);
        var markers = new List<Entity<ESDistributedSpawnerMarkerComponent>>();
        foreach (var marker in _markers)
        {
            if (marker.Comp.Id != ent.Comp.Id)
                continue;
            markers.Add(marker);
        }

        if (markers.Count == 0)
        {
            Log.Warning($"No available markers of ID {ent.Comp.Id} for spawner {ToPrettyString(ent)}");
            return;
        }
        _random.Shuffle(markers);

        var tables = new List<EntityTableSelector>(ent.Comp.Tables);
        var spawnPoints = Math.Min(markers.Count, ent.Comp.Tables.Count);
        var iterations = (int) MathF.Floor((float) ent.Comp.Tables.Count / spawnPoints);
        var picklist = new List<Entity<ESDistributedSpawnerMarkerComponent>>();

        // Calculate the remainder from dividing our iterations out into spawn points
        var extraIterations = tables.Count - iterations * spawnPoints;

        var spawns = new List<EntProtoId>();
        for (var i = 0; i < spawnPoints; i++)
        {
            if (picklist.Count == 0)
            {
                picklist.AddRange(markers);
            }

            var adjustedIterations = extraIterations-- > 0
                ? iterations + 1
                : iterations;

            spawns.Clear();
            for (var j = 0; j < adjustedIterations; j++)
            {
                if (tables.Count == 0)
                    break;
                var table = _random.PickAndTake(tables);
                spawns.AddRange(_entityTable.GetSpawns(table));
            }

            var marker = _random.PickAndTake(picklist);
            var coords = Transform(marker).Coordinates;
            EntityManager.SpawnAtPosition(spawns, coords, ent.Comp.Strategy, ent.Comp.Offset);
        }
    }
}
