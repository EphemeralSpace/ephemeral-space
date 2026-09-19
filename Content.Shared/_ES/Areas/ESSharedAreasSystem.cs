using System.Numerics;
using Content.Shared._ES.Areas.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Areas;

public abstract partial class ESSharedAreasSystem : EntitySystem
{
    [Dependency] private ChunkEntitySystem _chunkEntity = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {

    }

    public bool TrySetArea(EntityCoordinates coords, ProtoId<ESAreaPrototype>? area)
    {
        if (_transform.GetGrid(coords) is not { } grid ||
            !_gridQuery.TryComp(grid, out var gridComp))
            return false;

        var tile = _map.CoordinatesToTile(grid, gridComp, coords);
        var chent = GetChunkEntity(grid, coords.Position);
        if (area.HasValue)
            chent.Comp2.Areas[tile] = area.Value;
        else
            chent.Comp2.Areas.Remove(tile);
        DirtyChunk(chent);
        return true;
    }

    protected Entity<ChunkEntityComponent, ESAreaChunkComponent> GetChunkEntity(EntityUid grid, Vector2 coords)
    {
        var chent = _chunkEntity.GetOrCreateChunk(grid, ChunkEntitySystem.GetChunkIndices(coords));
        var comp = EnsureComp<ESAreaChunkComponent>(chent);
        return (chent, chent, comp);
    }

    protected void DirtyChunk(Entity<ChunkEntityComponent, ESAreaChunkComponent> chent)
    {
        if (chent.Comp2.Areas.Count == 0)
        {
            _chunkEntity.TryRemoveChunk((chent, chent.Comp1, MetaData(chent)));
            return;
        }

        Dirty(chent, chent.Comp2);
    }
}
