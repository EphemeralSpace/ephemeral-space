using System.Numerics;
using Content.Server._ES.Spawner.Components;
using Content.Shared.EntityTable;
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
        var xform = Transform(ent);

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

        var picklist = new List<Entity<ESDistributedSpawnerMarkerComponent>>();
        foreach (var table in ent.Comp.Tables)
        {
            if (picklist.Count == 0)
            {
                picklist.AddRange(markers);
            }

            var spawns = _entityTable.GetSpawns(table);
            var marker = _random.PickAndTake(picklist);
            var coords = Transform(marker).Coordinates;

            foreach (var proto in spawns)
            {
                // TODO: Spawn in a path, either horizontal or diagonal
                var xOffset = _random.NextFloat(-ent.Comp.Offset, ent.Comp.Offset);
                var yOffset = _random.NextFloat(-ent.Comp.Offset, ent.Comp.Offset);
                var trueCoords = coords.Offset(new Vector2(xOffset, yOffset));

                SpawnAtPosition(proto, trueCoords);
            }
        }
    }
}
