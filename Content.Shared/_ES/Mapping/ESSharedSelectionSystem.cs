using System.Numerics;
using JetBrains.Annotations;
using Robust.Shared.Containers;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Network;

namespace Content.Shared._ES.Mapping;

public abstract partial class ESSharedSelectionSystem : EntitySystem
{
    [Dependency] private SharedTransformSystem _xform = default!;
    [Dependency] private EntityLookupSystem _lookup = default!;
    [Dependency] private SharedMapSystem _map = default!;
    [Dependency] private SharedContainerSystem _container = default!;
    [Dependency] private INetManager _net = default!;

    [Dependency] private EntityQuery<TransformComponent> _xformQuery;
    [Dependency] private EntityQuery<MapGridComponent> _gridQuery;
    [Dependency] private EntityQuery<PlacementReplacementComponent> _replacementQuery;

    public override void Initialize()
    {
        base.Initialize();

        Subs.MappingEvents<ESMapperComponent>(subs =>
        {
            subs.Event<ESMappingDisableSelectionMessage>(OnDisableSelection);
            subs.Event<ESMappingEnableSelectionMessage>(OnEnableSelection);
            subs.Event<ESMappingStartSelectionMessage>(OnStartSelection);
            subs.Event<ESMappingMidSelectionMessage>(OnMidSelection);
            subs.Event<ESMappingStartMovingMessage>(OnStartMoving);
            subs.Event<ESMappingMidMovingMessage>(OnMidMoving);
            subs.Event<ESMappingStopMovingMessage>(OnStopMoving);
            subs.Event<ESMappingStopSelectionMessage>(OnStopSelection);
        });

        SubscribeLocalEvent<ESMapperComponent, ComponentGetState>(OnGetState);
        SubscribeLocalEvent<ESMapperComponent, ComponentHandleState>(OnHandleState);
    }

    [PublicAPI]
    public ESSelectionState GetSelectionState(ESNetSelectionState state)
    {
        return state switch
        {
            ESNetSelectionState.Disabled => new ESSelectionState.Disabled(),
            ESNetSelectionState.Idle => new ESSelectionState.Idle(),
            ESNetSelectionState.Selecting { Selection: var selection } => new ESSelectionState.Selecting(GetSelectionBox(selection)),
            ESNetSelectionState.Moving { Line: var line } => new ESSelectionState.Moving(GetLine(line)),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    [PublicAPI]
    public ESNetSelectionState GetNetSelectionState(ESSelectionState state)
    {
        return state switch
        {
            ESSelectionState.Disabled => new ESNetSelectionState.Disabled(),
            ESSelectionState.Idle => new ESNetSelectionState.Idle(),
            ESSelectionState.Selecting { Selection: var selection } => new ESNetSelectionState.Selecting(GetNetSelectionBox(selection)),
            ESSelectionState.Moving { Line: var line } => new ESNetSelectionState.Moving(GetNetLine(line)),
            _ => throw new ArgumentOutOfRangeException(),
        };
    }

    [PublicAPI]
    public ESSelectionBox GetSelectionBox(ESNetSelectionBox selection)
    {
        return new ESSelectionBox(GetEntity(selection.RelativeTo), selection.Start, selection.End, selection.Angle);
    }

    [PublicAPI]
    public ESNetSelectionBox GetNetSelectionBox(ESSelectionBox selection)
    {
        return new ESNetSelectionBox(GetNetEntity(selection.RelativeTo),
            selection.Start,
            selection.End,
            selection.Angle);
    }

    [PublicAPI]
    public ESLine GetLine(ESNetLine line)
    {
        return new ESLine(GetEntity(line.RelativeTo), line.Start, line.End);
    }

    [PublicAPI]
    public ESNetLine GetNetLine(ESLine selection)
    {
        return new ESNetLine(GetNetEntity(selection.RelativeTo),
            selection.Start,
            selection.End);
    }

    [PublicAPI]
    public (MapId, Box2Rotated) ToWorldBox2Rotated(ESSelectionBox selBox)
    {
        var mapStart = _xform.ToMapCoordinates(new EntityCoordinates(selBox.RelativeTo, selBox.Start));
        var mapEnd = _xform.ToMapCoordinates(new EntityCoordinates(selBox.RelativeTo, selBox.End));

        var localCurrent = mapStart.Position + selBox.Angle.RotateVec(mapEnd.Position - mapStart.Position);
        var box = Box2.FromTwoPoints(mapStart.Position, localCurrent);
        return (mapStart.MapId, new Box2Rotated(box, -selBox.Angle, mapStart.Position));
    }

    [PublicAPI]
    public HashSet<EntityUid> SelectEntities(ESSelectionBox selBox)
    {
        var (map, box) = ToWorldBox2Rotated(selBox);
        var (x, y) = box.Box.Size;
        if (x <= 0.1f || y <= 0.1f)
            return new();

        var result = _lookup.GetEntitiesIntersecting(map, box, LookupFlags.Uncontained);
        result.RemoveWhere(entity => !box.Contains(_xform.GetWorldPosition(entity)));
        return result;
    }

    /// <summary>
    /// The first grid the selection box intersects, if any.
    /// </summary>
    [PublicAPI]
    public Entity<MapGridComponent>? GetSelectedGrid(ESSelectionBox selBox)
    {
        var (mapId, worldBox) = ToWorldBox2Rotated(selBox);

        if (mapId == MapId.Nullspace)
            return null;

        var grids = new List<Entity<MapGridComponent>>();
        _map.FindGridsIntersecting(mapId, worldBox, ref grids, includeMap: false);

        return grids.Count > 0 ? grids[0] : null;
    }

    private void OnGetState(Entity<ESMapperComponent> ent, ref ComponentGetState args)
    {
        ent.Comp.ActiveEntitySelection?.RemoveWhere(uid => TerminatingOrDeleted(uid));

        args.State = new ESMapperComponentState
        {
            SelectionState = GetNetSelectionState(ent.Comp.SelectionState),
            ActiveGridSelection = ent.Comp.ActiveGridSelection.HasValue ? GetNetSelectionBox(ent.Comp.ActiveGridSelection.Value) : null,
            ActiveEntitySelection = ent.Comp.ActiveEntitySelection is not null ? GetNetEntitySet(ent.Comp.ActiveEntitySelection) : null,
        };
    }

    private void OnHandleState(Entity<ESMapperComponent> ent, ref ComponentHandleState args)
    {
        if (args.Current is not ESMapperComponentState state)
            return;

        ent.Comp.SelectionState = GetSelectionState(state.SelectionState);
        ent.Comp.ActiveEntitySelection =
            state.ActiveEntitySelection is not null ? GetEntitySet(state.ActiveEntitySelection) : null;
        ent.Comp.ActiveGridSelection =
            state.ActiveGridSelection.HasValue ? GetSelectionBox(state.ActiveGridSelection.Value) : null;
        OnStateUpdated(ent);
    }

    protected virtual void OnStateUpdated(Entity<ESMapperComponent> ent)
    {
    }

    private void OnDisableSelection(Entity<ESMapperComponent> ent, ref ESMappingDisableSelectionMessage args)
    {
        ent.Comp.SelectionState = new ESSelectionState.Disabled();
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnEnableSelection(Entity<ESMapperComponent> ent, ref ESMappingEnableSelectionMessage args)
    {
        ent.Comp.SelectionState = new ESSelectionState.Idle();
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnStartSelection(Entity<ESMapperComponent> ent, ref ESMappingStartSelectionMessage args)
    {
        var (relativeTo, position) = GetCoordinates(args.StartPosition);
        ent.Comp.SelectionState = new ESSelectionState.Selecting(new ESSelectionBox(relativeTo, position, position, args.Angle));
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnMidSelection(Entity<ESMapperComponent> ent, ref ESMappingMidSelectionMessage args)
    {
        if (ent.Comp.SelectionState is not ESSelectionState.Selecting { Selection: var currentSelection })
            return;

        var endCoordinates = GetCoordinates(args.EndPosition);
        var remapped = _xform.WithEntityId(endCoordinates, currentSelection.RelativeTo);

        ent.Comp.SelectionState = new ESSelectionState.Selecting(currentSelection with { End = remapped.Position });
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnStopSelection(Entity<ESMapperComponent> ent, ref ESMappingStopSelectionMessage args)
    {
        if (ent.Comp.SelectionState is not ESSelectionState.Selecting { Selection: var currentSelection })
            return;

        var rot = ToWorldBox2Rotated(currentSelection);
        var (x, y) = rot.Item2.Box.Size;
        if (x > 0.1f && y > 0.1f)
        {
            if (GetSelectedGrid(currentSelection).HasValue)
                ent.Comp.ActiveGridSelection = currentSelection;
            else
                ent.Comp.ActiveGridSelection = null;

            ent.Comp.ActiveEntitySelection = SelectEntities(currentSelection);
        }
        else
        {
            ent.Comp.ActiveGridSelection = null;
            ent.Comp.ActiveEntitySelection = null;
        }

        ent.Comp.SelectionState = new ESSelectionState.Idle();
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnStartMoving(Entity<ESMapperComponent> ent, ref ESMappingStartMovingMessage args)
    {
        var (relativeTo, position) = GetCoordinates(args.StartPosition);
        ent.Comp.SelectionState = new ESSelectionState.Moving(new ESLine(relativeTo, position, position));
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnMidMoving(Entity<ESMapperComponent> ent, ref ESMappingMidMovingMessage args)
    {
        if (ent.Comp.SelectionState is not ESSelectionState.Moving { Line: var currentLine })
            return;

        var endCoordinates = GetCoordinates(args.EndPosition);
        var remapped = _xform.WithEntityId(endCoordinates, currentLine.RelativeTo);

        ent.Comp.SelectionState = new ESSelectionState.Moving(currentLine with { End = remapped.Position });
        OnStateUpdated(ent);
        Dirty(ent);
    }

    private void OnStopMoving(Entity<ESMapperComponent> ent, ref ESMappingStopMovingMessage args)
    {
        if (ent.Comp.SelectionState is not ESSelectionState.Moving { Line: var currentLine })
            return;

        if (Vector2.DistanceSquared(currentLine.Start, currentLine.End) > 0.15f)
        {
            ent.Comp.ActiveGridSelection =
                Move(ent.Comp.ActiveGridSelection, ent.Comp.ActiveEntitySelection, currentLine);
        }

        ent.Comp.SelectionState = new ESSelectionState.Idle();
        OnStateUpdated(ent);
        Dirty(ent);
    }

    /// <summary>
    /// Moves the given grid tiles and entities by the given line offset
    /// </summary>
    /// <param name="grid">Selection box specifying the tiles on one (1) grid only to move</param>
    /// <param name="entities">List of entities to offset</param>
    /// <param name="offsetBy">The line to offset tiles and entities along</param>
    /// <returns>The new selection box after movement, if any</returns>
    [PublicAPI]
    public ESSelectionBox? Move(ESSelectionBox? grid, HashSet<EntityUid>? entities, ESLine offsetBy)
    {
        var mapStart = _xform.ToMapCoordinates(new EntityCoordinates(offsetBy.RelativeTo, offsetBy.Start));
        var mapEnd = _xform.ToMapCoordinates(new EntityCoordinates(offsetBy.RelativeTo, offsetBy.End));
        var worldDelta = mapEnd.Position - mapStart.Position;

        if (grid is not { } selBox || GetSelectedGrid(selBox) is not { } gridEnt)
        {
            return MoveWithoutTiles(grid, entities, worldDelta);
        }

        return MoveWithTiles(selBox, entities, worldDelta, gridEnt);
    }

    private ESSelectionBox? MoveWithoutTiles(ESSelectionBox? grid, HashSet<EntityUid>? entities, Vector2 worldDelta)
    {
        foreach (var entry in GetValidEntitiesToMove(entities, null))
        {
            OffsetEntity(entry, worldDelta);
        }

        if (grid is not { } freestandingBox)
            return null;

        var freeDelta = (-_xform.GetWorldRotation(freestandingBox.RelativeTo)).RotateVec(worldDelta);
        return freestandingBox with { Start = freestandingBox.Start + freeDelta, End = freestandingBox.End + freeDelta };
    }

    private ESSelectionBox? MoveWithTiles(ESSelectionBox gridSelBox, HashSet<EntityUid>? entities, Vector2 worldDelta, Entity<MapGridComponent> gridEnt)
    {
        var gridRot = _xform.GetWorldRotation(gridEnt.Owner);

        var localDelta = (-gridRot).RotateVec(worldDelta) / gridEnt.Comp.TileSize;
        var tileOffset = new Vector2i((int) MathF.Round(localDelta.X), (int) MathF.Round(localDelta.Y));

        if (tileOffset == Vector2i.Zero)
            return gridSelBox;

        var snappedLocal = (Vector2) tileOffset * gridEnt.Comp.TileSize;
        var snappedWorld = gridRot.RotateVec(snappedLocal);

        if (_net.IsServer)
            MoveTiles(gridEnt, gridSelBox, entities, tileOffset, snappedLocal, snappedWorld);

        var boxDelta = (-_xform.GetWorldRotation(gridSelBox.RelativeTo)).RotateVec(snappedWorld);
        return gridSelBox with { Start = gridSelBox.Start + boxDelta, End = gridSelBox.End + boxDelta };
    }

    private void MoveTiles(
        Entity<MapGridComponent> gridEnt,
        ESSelectionBox selBox,
        HashSet<EntityUid>? entities,
        Vector2i tileOffset,
        Vector2 snappedLocal,
        Vector2 snappedWorld)
    {
        var sources = GetSourceTiles(gridEnt, selBox);

        var destinationTiles = new List<(Vector2i, Tile)>(sources.Count);
        foreach (var (index, tile) in sources)
        {
            destinationTiles.Add((index + tileOffset, tile));
        }

        var moved = GetValidEntitiesToMove(entities, gridEnt.Owner);

        var couldSplit = gridEnt.Comp.CanSplit;
        gridEnt.Comp.CanSplit = false;

        _map.SetTiles(gridEnt, destinationTiles);

        foreach (var entry in moved)
        {
            if (TerminatingOrDeleted(entry.Entity))
                continue;

            if (entry.LocalPosition is { } localPosition)
            {
                var anchored = entry.Entity.Comp.Anchored;
                var oldTile = _map.TileIndicesFor(gridEnt, gridEnt, entry.Entity.Comp.Coordinates);

                if (anchored)
                    _map.RemoveFromSnapGridCell(gridEnt.Owner, gridEnt.Comp, oldTile, entry.Entity.Owner);

                _xform.SetCoordinates(entry.Entity, entry.Entity, new EntityCoordinates(gridEnt.Owner, localPosition + snappedLocal), unanchor: false);

                if (anchored)
                    _map.AddToSnapGridCell(gridEnt.Owner, gridEnt.Comp, oldTile + tileOffset, entry.Entity.Owner);
            }
            else
            {
                OffsetEntity(entry, snappedWorld);
            }

            ReplaceAt(gridEnt, entry.Entity, entities);
        }

        var cleared = new List<(Vector2i, Tile)>(sources.Count);
        foreach (var (index, _) in sources)
        {
            cleared.Add((index, Tile.Empty));
        }

        _map.SetTiles(gridEnt, cleared);

        gridEnt.Comp.CanSplit = couldSplit;
    }

    private List<(Vector2i Indices, Tile Tile)> GetSourceTiles(Entity<MapGridComponent> gridEnt, ESSelectionBox selBox)
    {
        var tileSize = gridEnt.Comp.TileSize;

        var (_, worldBox) = ToWorldBox2Rotated(selBox);
        var localBox = _xform.GetInvWorldMatrix(gridEnt.Owner).TransformBox(worldBox);

        var sources = new List<(Vector2i Indices, Tile Tile)>();
        foreach (var tileRef in _map.GetLocalTilesIntersecting(gridEnt.Owner, gridEnt.Comp, localBox, false))
        {
            var topLeft = new Vector2(tileRef.GridIndices.X, tileRef.GridIndices.Y) * tileSize;
            var center = topLeft + new Vector2(tileSize / 2f);

            if (!localBox.Contains(center))
                continue;

            sources.Add((tileRef.GridIndices, tileRef.Tile));
        }

        return sources;
    }

    private List<ESMovingEntity> GetValidEntitiesToMove(HashSet<EntityUid>? entities, EntityUid? grid)
    {
        var moved = new List<ESMovingEntity>(entities?.Count ?? 0);

        if (entities is null)
            return moved;

        foreach (var uid in entities)
        {
            if (uid == grid || TerminatingOrDeleted(uid) || !_xformQuery.TryComp(uid, out var xform))
                continue;

            if (_gridQuery.HasComp(uid) || HasComp<MapComponent>(uid) || _container.IsEntityInContainer(uid))
                continue;

            if (!xform.ParentUid.IsValid() || xform.MapUid is null)
                continue;

            var localPosition = grid is not null && xform.ParentUid == grid ? xform.LocalPosition : (Vector2?) null;
            moved.Add(new ESMovingEntity((uid, xform), localPosition));
        }

        return moved;
    }

    private void OffsetEntity(ESMovingEntity entry, Vector2 worldDelta)
    {
        if (TerminatingOrDeleted(entry.Entity) || !entry.Entity.Comp.ParentUid.IsValid() || entry.Entity.Comp.MapUid is null)
            return;

        _xform.SetWorldPosition(entry.Entity, _xform.GetWorldPosition(entry.Entity.Comp) + worldDelta);
    }

    private readonly record struct ESMovingEntity(
        Entity<TransformComponent> Entity,
        Vector2? LocalPosition);

    private void ReplaceAt(Entity<MapGridComponent> grid, Entity<TransformComponent> ent, HashSet<EntityUid>? ignore)
    {
        if (!_replacementQuery.TryComp(ent.Owner, out var replacement))
            return;

        var indices = _map.TileIndicesFor(grid, grid, ent.Comp.Coordinates);

        foreach (var anchored in _map.GetAnchoredEntities(grid.Owner, grid.Comp, indices))
        {
            if (anchored == ent.Owner || ignore?.Contains(anchored) == true)
                continue;

            if (!_replacementQuery.TryComp(anchored, out var otherReplacement) || otherReplacement.Key != replacement.Key)
                continue;

            PredictedQueueDel(anchored);
        }
    }
}
