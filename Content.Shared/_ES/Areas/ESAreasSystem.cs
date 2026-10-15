using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Numerics;
using Content.Shared._ES.Areas.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Areas;

public sealed partial class ESAreasSystem : EntitySystem
{
    [Dependency] private INetManager _net = default!;
    [Dependency] private ChunkEntitySystem _chunkEntity = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<GridInitializeEvent>(OnGridInit);
    }

    // TODO: testing, seed grids with random areas
    private void OnGridInit(GridInitializeEvent ev)
    {
        var rand = new RobustRandom();
        var areas = ProtoMan.EnumeratePrototypes<ESAreaPrototype>().ToList();
        foreach (var tile in _map.GetAllTiles(ev.EntityUid, ev.Grid))
        {
            TrySetArea((ev.EntityUid, ev.Grid), tile.GridIndices,rand.Pick(areas));
        }
    }

    public bool TrySetArea(EntityCoordinates coords, ProtoId<ESAreaPrototype>? area)
    {
        if (_transform.GetGrid(coords) is not { } grid || !_gridQuery.TryComp(grid, out var gridComp))
            return false;

        var tile = _map.CoordinatesToTile(grid, gridComp, coords);
        return TrySetArea((grid, gridComp), tile, area);
    }

    public bool TrySetArea(Entity<MapGridComponent> ent, Vector2i indices, ProtoId<ESAreaPrototype>? area)
    {
        if (!TryGetChunkEntity(ent, indices, out var chent))
            return false;

        if (area.HasValue)
            chent.Value.Comp2.Areas[indices] = area.Value;
        else
            chent.Value.Comp2.Areas.Remove(indices);
        DirtyChunk(chent.Value);
        return true;
    }

    public ProtoId<ESAreaPrototype>? GetAreaOrNull(Entity<TransformComponent?> ent)
    {
        TryGetArea(ent, out var area);
        return area;
    }

    public ProtoId<ESAreaPrototype>? GetAreaOrNull(EntityCoordinates coords)
    {
        TryGetArea(coords, out var area);
        return area;
    }

    public bool TryGetArea(Entity<TransformComponent?> ent, [NotNullWhen(true)] out ProtoId<ESAreaPrototype>? area)
    {
        area = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.GridUid is not { } grid || !_gridQuery.TryComp(grid, out var gridComp))
            return false;

        var tile = _map.CoordinatesToTile(grid, gridComp, ent.Comp.Coordinates);
        return TryGetArea((grid, gridComp), tile, out area);
    }

    public bool TryGetArea(EntityCoordinates coords, [NotNullWhen(true)] out ProtoId<ESAreaPrototype>? area)
    {
        area = null;
        if (_transform.GetGrid(coords) is not { } grid || !_gridQuery.TryComp(grid, out var gridComp))
            return false;

        var tile = _map.CoordinatesToTile(grid, gridComp, coords);
        return TryGetArea((grid, gridComp), tile, out area);
    }

    public bool TryGetArea(Entity<MapGridComponent> ent, Vector2i indices, [NotNullWhen(true)] out ProtoId<ESAreaPrototype>? area)
    {
        area = null;

        if (!TryGetChunkEntity(ent, indices, out var chent))
            return false;

        if (!chent.Value.Comp2.Areas.TryGetValue(indices, out var lookup))
            return false;

        area = lookup;
        return true;
    }

    private bool TryGetChunkEntity(EntityUid grid, Vector2 coords, [NotNullWhen(true)] out Entity<ChunkEntityComponent, ESAreaChunkComponent>? chent)
    {
        chent = null;

        // Client should not initialize new chunk entities,
        // so we need to fail if we try and access a nonexistent chunk ent from the client.
        // This also means chunk ent creation can't be predicted, but tbh you could never predict
        // entity spawning to begin with.
        if (_net.IsClient)
        {
            if (!_chunkEntity.TryGetChunk(grid, ChunkEntitySystem.GetChunkIndices(coords), out var ent))
                return false;

            chent = (ent.Value, ent.Value, EnsureComp<ESAreaChunkComponent>(ent.Value));
            return true;
        }

        chent = GetChunkEntity(grid, coords);
        return true;
    }

    private Entity<ChunkEntityComponent, ESAreaChunkComponent> GetChunkEntity(EntityUid grid, Vector2 coords)
    {
        var chent = _chunkEntity.GetOrCreateChunk(grid, ChunkEntitySystem.GetChunkIndices(coords));
        var comp = EnsureComp<ESAreaChunkComponent>(chent);
        return (chent, chent, comp);
    }

    private void DirtyChunk(Entity<ChunkEntityComponent, ESAreaChunkComponent> chent)
    {
        if (chent.Comp2.Areas.Count == 0)
        {
            _chunkEntity.TryRemoveChunk((chent, chent.Comp1, MetaData(chent)));
            return;
        }

        Dirty(chent, chent.Comp2);
    }

    public override void Update(float frameTime)
    {
        foreach (var (uid, area, xform) in EntityQueryEnumerator<ESAreaTrackingComponent, TransformComponent>())
        {
            var newArea = GetAreaOrNull((uid, xform));

            if (area.Area == newArea)
                continue;

            var oldArea = area.Area;
            area.Area = newArea;
            Dirty(uid, area);
            var ev = new ESAreaChangedEvent(oldArea, newArea);
            RaiseLocalEvent(uid, ref ev);
        }
    }
}
