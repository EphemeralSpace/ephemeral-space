using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Client._ES.LocalPlayer;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Content.Client._ES.SoundOcclusion;

public sealed class TomenoSoundOcclusionSystem : EntitySystem
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IOverlayManager _overlayManager = default!;

    private SharedMapSystem _sharedMapSystem = default!;
    private TransformSystem _transformSystem = default!;
    private TurfSystem _turfSystem = default!;

    private EntityQuery<ESLocalPlayerMarkerComponent> _localPlayerMarkerQuery;
    private EntityQuery<AirtightComponent> _airtightQuery;
    private EntityQuery<TransformComponent> _transformQuery;
    private EntityQuery<MapGridComponent> _mapGridQuery;
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<AirtightComponent, ComponentInit>(OnAirtightInit);
        SubscribeLocalEvent<AirtightComponent, ComponentShutdown>(OnAirtightShutdown);
        // for all intents and purposes an unanchor = remove and anchor = add
        SubscribeLocalEvent<AirtightComponent, AnchorStateChangedEvent>(OnAirtightAnchorChange);
        SubscribeLocalEvent<AirtightComponent, ReAnchorEvent>(OnAirtightReAnchor);
        SubscribeLocalEvent<AirtightComponent, MoveEvent>(OnAirtightMove);
        SubscribeLocalEvent<AirtightComponent, AfterAutoHandleStateEvent>(OnAirtightStateChange);

        SubscribeLocalEvent<ESLocalPlayerMarkerComponent, MoveEvent>(OnPlayerMove);

        _sharedMapSystem = _entityManager.System<SharedMapSystem>();
        _transformSystem = _entityManager.System<TransformSystem>();
        _turfSystem = _entityManager.System<TurfSystem>();

        _localPlayerMarkerQuery = GetEntityQuery<ESLocalPlayerMarkerComponent>();
        _airtightQuery = GetEntityQuery<AirtightComponent>();
        _transformQuery = GetEntityQuery<TransformComponent>();
        _mapGridQuery = GetEntityQuery<MapGridComponent>();

        _overlayManager.AddOverlay(new TomenoSoundOcclusionOverlay());
    }

    public override void Shutdown()
    {
        base.Shutdown();

        _overlayManager.RemoveOverlay<TomenoSoundOcclusionOverlay>();
    }

    /* TODO list:
    - X PVS-sized, grid-aligned object tilemap for pathfinding data, boolean tilemap for airtight
        - X should we just use a Vector2i dictionary for occlusion and query for all airtight?
    - X Fill boolean tilemap with airtight data
        - X should i just grab all the airtight in pvs and slot it in?
        - X how to handle offgrid? multigrid?
            - X offgrid - space is occluded, planet is not
            - X multigrid - translate to local grid coordinates - good enough for docked shuttles
    - X make an overlay for this data
    -- Algorithm
    - X Flood fill from the listener pos
        - X breadth-first
        - X pathfinding data?
            - X coordinates to previous tile?
    - X tracing against the tilemap/checking "los" - we use supercover dda
    - X pathfinding sounds
        - check los first
    - shorten path - X mostly done - doesn't matter for now
        - X from either side with sound traces? should be fine to step across the path for now - since we use diagonals, yes
            - maybe binary search or smt later
            - last visible tile from hearer/listener becomes the "portal" - for updating gotten paths later
            - shorten from listener first
    -- Final stretch
    - cleanup! make structures instead of system vars! split stuff!
    - when the soundstage becomes dirty (airtights move, etc), mark it as dirty and update on next client tick (Update)
    - this means we will need to snapshot all grid coordinate system info alongside the soundstage?
    - when a new soundstage is generated, schedule a background task thread to calculate the new paths
        - schedule using System.Threading.Tasks, save it
            - for cancellation, getting the
        - we can only update the sounds/soundpaths up to 20 times a second - don't gen stages faster than this!
    - on next update, if the task is finished, it returns a new floodfill - push it into the system as new state
        - track "soundstage generation", every time theres a new one we increment this
    - SoundStage - basic occlusion data structure, includes grid info
    - SoundPaths - structure returned by the floodfiller, includes SoundStage
    - UpdatablePath
        - emitterPortal, listenerPortal, portalDistance - distance between portals, soundstage generation
        - new occluded dist: emitter-eportal + portaldistance + listener-lportal
        - only 1 portal -> both distances from portals? no portal -> no occlusion
        - sound only needs to grab a new path if the soundstage gen is different
    */

    public int SoundStageGeneration = 0;

    // Used for tracking significant movement updates
    public Vector2i? LastUpdatedTile = null;
    public EntityUid? LastGridUid = null;
    // Used for updating paths
    public Vector2? LastLocalPos = null;
    // ya
    public SoundPaths? CurrentSoundPaths = null;

    private bool _dirtySoundStage = true;

    private Task<SoundPaths>? SoundPathsTask = null;

    public sealed class SoundStage
    {
        // occlusion data
        public required Dictionary<Vector2i, bool> Passable { get; init; }

        // grid snapshot — so a worker thread never touches components
        public required EntityUid GridUid { get; init; }
        public required MapId MapId { get; init; }

        // Will we need this later?
        // I guess so, in case the grid becomes invalid and the soundstage is still active
        public required ushort TileSize { get; init; }
        public required Matrix3x2 WorldMatrix { get; init; }
        public required Matrix3x2 InvWorldMatrix { get; init; }

        public required bool IsPlanet { get; init; }   // decides the default for unknown tiles

        // listener
        public required Vector2i ListenerTile { get; init; }

        // Do we need this? We will need to update the listener pos live
        public required Vector2 ListenerPos { get; init; }   // grid-local

        public required int Generation { get; init; }

        public bool IsPassable(Vector2i position)
        {
            return Passable.GetValueOrDefault(position, IsPlanet);
        }

        public Vector2 WorldToLocal(Vector2 posWorld)
        {
            return Vector2.Transform(posWorld, InvWorldMatrix);
        }

        public Vector2 LocalToWorld(Vector2 posLocal)
        {
            return Vector2.Transform(posLocal, WorldMatrix);
        }

        public Vector2i LocalToTile(Vector2 posLocal)
        {
            // honestly TileSize is a no-op 100% of the time right now (AFAIK)
            var x = (int)Math.Floor(posLocal.X / TileSize);
            var y = (int)Math.Floor(posLocal.Y / TileSize);
            return new Vector2i(x, y);
        }

        public Vector2i WorldToTile(Vector2 posWorld)
        {
            return LocalToTile(WorldToLocal(posWorld));
        }

        // Supercover DDA
        public bool CheckVisibility(Vector2 from, Vector2 to)
        {
            var t1 = to.Floored();
            var t2 = from.Floored();

            var dX = from.X - to.X;
            var dY = from.Y - to.Y;

            var sX = dX > 0 ? 1 : -1;
            var sY = dY > 0 ? 1 : -1;

            var tDeltaX = dX != 0 ? Math.Abs(1f / dX) : float.PositiveInfinity;
            var tDeltaY = dY != 0 ? Math.Abs(1f / dY) : float.PositiveInfinity;

            var tMaxX = dX > 0 ? ((t1.X + 1 - to.X) / dX) : (dX < 0 ? (t1.X - to.X) / dX : float.PositiveInfinity);
            var tMaxY = dY > 0 ? ((t1.Y + 1 - to.Y) / dY) : (dY < 0 ? (t1.Y - to.Y) / dY : float.PositiveInfinity);

            // If this was the proper algo then here you would check the start tile, but I think it's better if we don't
            //   for the sake of sound emitters within walls

            if (!IsPassable(t2))
            {
                return false;
            }

            var steps = Math.Abs(t2.X - t1.X) + Math.Abs(t2.Y - t1.Y);
            for (var i = 0; i < steps; i++)
            {
                if (tMaxX < tMaxY)
                {
                    tMaxX += tDeltaX;
                    t1.X += sX;
                }
                else
                {
                    tMaxY += tDeltaY;
                    t1.Y += sY;
                }

                // TODO: Abstract every instance of Soundstage.TryGetValue for planetmaps
                if (!IsPassable(t1))
                {
                    return false;
                }
            }

            return true;
        }
    }

    public sealed class PathResult
    {
        // Portal locations - if they're not
        public Vector2? EmitterPortal { get; init; }
        public Vector2? ListenerPortal { get; init; }

        // Pathed distance between Emitter and Listener portals
        public float PortalDistance { get; init; }

        // For invalidating the path
        public int Generation { get; init; }
        public Vector2i EmitterTile { get; init; }

        // DEBUG!
        public required List<Vector2i> Path { get; init; }
    }

    public sealed class SoundPaths
    {
        public required SoundStage Stage { get; init; }
        public required Dictionary<Vector2i, Vector2i?> Paths { get; init; }
    }

    public PathResult? FindPath(Vector2 emitter)
    {
        if (CurrentSoundPaths == null || !LastLocalPos.HasValue)
            return null;

        var emitterTile = CurrentSoundPaths.Stage.LocalToTile(emitter);

        if (!CurrentSoundPaths.Paths.ContainsKey(emitterTile))
            return null;

        // for sounds coming from inside occluders:
        // middle of the tile: follow soundpath
        // else: nearest unoccluded tile
        if (!CurrentSoundPaths.Stage.IsPassable(emitterTile))
        {
            var emitterCenterDelta = (emitterTile + Vector2.One / 2) - emitter;
            var newEmitterTile = CurrentSoundPaths.Paths[emitterTile];

            if (newEmitterTile == null)
                newEmitterTile = LastLocalPos.Value.Floored(); // TODO: this should just be some sort of short-circuit?

            if (emitterCenterDelta.X >= -0.25f && emitterCenterDelta.X <= 0.25f
                && emitterCenterDelta.Y >= -0.25f && emitterCenterDelta.Y <= 0.25f)
            {
                emitterTile = newEmitterTile.Value;
            }
            else
            {
                var bestDistance = float.MaxValue;
                // N
                Func<Vector2i, bool> checkCandidate = (candidate) =>
                {
                    if (CurrentSoundPaths.Stage.IsPassable(candidate))
                    {
                        var candidateDistance = ((candidate + Vector2.One / 2f) - emitter).Length();
                        if (candidateDistance < bestDistance)
                        {
                            bestDistance = candidateDistance;
                            newEmitterTile = candidate;
                            return true;
                        }
                    }
                    return false;
                };
                checkCandidate(emitterTile + Vector2i.Up);
                checkCandidate(emitterTile + Vector2i.Right);
                checkCandidate(emitterTile + Vector2i.Down);
                checkCandidate(emitterTile + Vector2i.Left);

                if (!CurrentSoundPaths.Paths.ContainsKey(newEmitterTile.Value))
                    return null;

                emitterTile = newEmitterTile.Value;
            }
        }

        if (CurrentSoundPaths.Stage.CheckVisibility(emitter, LastLocalPos.Value))
        {
            return new PathResult
            {
                EmitterPortal = null,
                ListenerPortal = null,
                PortalDistance = 0,
                Generation = CurrentSoundPaths.Stage.Generation,
                EmitterTile = emitterTile,
                Path = new List<Vector2i>(), // DEBUG!
            };
        }

        var pathTiles = new List<Vector2i>(); // TODO: We can actually initialize this with the exact capacity

        var curTile = emitterTile;
        while (CurrentSoundPaths.Paths.TryGetValue(curTile, out var pathTile))
        {
            pathTiles.Add(curTile);

            if (pathTile == null)
                break;

            curTile = pathTile.Value;
        }

        if (pathTiles.Count == 0)
            return null;

        // Simple "portal" special cases
        // TODO: can this be cleaned up somehow?
        if (pathTiles.Count <= 2)
        {
            return new PathResult
            {
                EmitterPortal = null,
                ListenerPortal = null,
                PortalDistance = 0,
                Generation = CurrentSoundPaths.Stage.Generation,
                EmitterTile = emitterTile,
                Path = new List<Vector2i>(), // DEBUG!
            };
        }
        else if (pathTiles.Count == 3)
        {
            // Middle tile is both portals
            return new PathResult
            {
                EmitterPortal = pathTiles[1],
                ListenerPortal = pathTiles[1],
                PortalDistance = 0,
                Generation = CurrentSoundPaths.Stage.Generation,
                EmitterTile = emitterTile,
                Path = new List<Vector2i>(), // DEBUG!
            };
        }

        /*
        var emitterPortalPos = 1; // Tile after emitter
        var listenerPortalPos = pathTiles.Count - 2; // Tile before listener
        */

        // Time to find the "portals" for the listener and then the emitter
        var emitterPortalPos = 1; // Tile after emitter
        var listenerPortalPos = pathTiles.Count - 2; // Tile before listener


        // var listenerPortalPos = pathTiles.Count - 1;
        for (var i = listenerPortalPos; i >= emitterPortalPos; i--)
        {
            var nextTile = pathTiles[i];
            if (CurrentSoundPaths.Stage.CheckVisibility(nextTile + Vector2.One / 2, CurrentSoundPaths.Stage.LocalToTile(LastLocalPos.Value) + Vector2.One / 2))
            {
                listenerPortalPos = i;
            }
        }
        //
        // var emitterPortalPos = 0;
        for (var i = emitterPortalPos; i <= listenerPortalPos; i++)
        {
            var nextTile = pathTiles[i];
            if (CurrentSoundPaths.Stage.CheckVisibility(nextTile + Vector2.One / 2, emitterTile + Vector2.One / 2))
            {
                emitterPortalPos = i;
            }
        }


        // Measure span between portals
        float portalDistance = 0;
        Vector2i previousTile = pathTiles[emitterPortalPos] ;
        for (var i = emitterPortalPos; i <= listenerPortalPos; i++)
        {
            var nextTile = pathTiles[i] ;
            portalDistance += Vector2.Distance(previousTile, nextTile);
            previousTile = nextTile;
        }

        return new PathResult
        {
            EmitterPortal = pathTiles[emitterPortalPos],
            ListenerPortal = pathTiles[listenerPortalPos],
            PortalDistance = portalDistance,
            Generation = CurrentSoundPaths.Stage.Generation,
            EmitterTile = emitterTile,
            Path = pathTiles.Slice(emitterPortalPos, (listenerPortalPos - emitterPortalPos) + 1), // DEBUG!
        };



        // var pathPoints = new List<Vector2>();
        // pathPoints.Add(emitter);
        //
        // // Time to find the "portal" for the emitter and then the listener
        // var emitterPortalPos = 0;
        // for (var i = 1; i < pathTiles.Count; i++)
        // {
        //     var nextTile = pathTiles[i];
        //     if (CheckVisibility(nextTile + Vector2.One / 2, pos))
        //     {
        //         emitterPortalPos = i;
        //     }
        // }
        //
        // var listenerPortalPos = pathTiles.Count - 1;
        // for (var i = pathTiles.Count - 1; i >= emitterPortalPos; i--)
        // {
        //     var nextTile = pathTiles[i];
        //     if (CheckVisibility(nextTile + Vector2.One / 2, LastLocalPos.Value))
        //     {
        //         listenerPortalPos = i;
        //     }
        // }
        //
        // for (var i = emitterPortalPos; i <= listenerPortalPos; i++)
        // {
        //     pathPoints.Add(pathTiles[i] + Vector2.One / 2);
        // }
        //
        // pathPoints.Add(LastLocalPos.Value);
        //
        // var rawPathDistance = pathTiles.Count;
        // float finalDistance = 0;
        // var lastPoint = pos;
        // foreach (var pathPoint in pathPoints)
        // {
        //     finalDistance += Vector2.Distance(lastPoint, pathPoint);
        // }
    }

    private SoundStage? SnapshotStage()
    {
        Dictionary<Vector2i, bool> passability = new();

        // Get local player
        var localQuery = EntityQueryEnumerator<ESLocalPlayerMarkerComponent>();
        if (!localQuery.MoveNext(out var localUid, out _))
            return null;
        localQuery.Dispose();

        if (!_transformQuery.TryGetComponent(localUid, out var transform))
            return null;

        EntityUid gridUid;
        if (transform.GridUid == null)
        {
            // TODO: Find the nearest grid? Only matters on planets tho, we don't have them yet
            //var mapUid = transform.MapUid;
            return null;
        }
        else
        {
            gridUid = (EntityUid) transform.GridUid;
        }

        if (!_mapGridQuery.TryGetComponent(gridUid, out var grid))
            return null;

        var localGridTile = _sharedMapSystem.TileIndicesFor(gridUid, grid, transform.Coordinates);
        var localGridPos = _sharedMapSystem.LocalToGrid(gridUid, grid, transform.Coordinates);

        // if (!LocalGridTile.HasValue || !LocalGridPos.HasValue)
        //     return null;

        var soundRange = 15;
        var tileRange = Box2.CenteredAround((Vector2)localGridPos, new Vector2(1 + soundRange * 2, 1 + soundRange * 2));

        var tilesEnumerator = _sharedMapSystem.GetLocalTilesEnumerator(gridUid, grid, tileRange, true);

        while (tilesEnumerator.MoveNext(out var tile))
        {
            if (!_turfSystem.IsSpace(tile.Tile))
                passability[tile.GridIndices] = true;
        }

        var airtightQuery = EntityQueryEnumerator<AirtightComponent>();
        while (airtightQuery.MoveNext(out var uid, out var comp))
        {
            if (!comp.AirBlocked)
                continue;

            if (!_transformQuery.TryGetComponent(uid, out var thisTransform))
                continue;

            // TODO: Can these be offgrid? How does planet stuff work?
            // I think this will work, LocalToTile converts to world cords internally
            passability[_sharedMapSystem.LocalToTile(gridUid, grid, thisTransform.Coordinates)] = false;
        }

        return new SoundStage
        {
            Passable = passability,
            GridUid = gridUid,
            MapId = transform.MapID,
            TileSize = grid.TileSize,
            WorldMatrix = _transformSystem.GetWorldMatrix(gridUid),
            InvWorldMatrix = _transformSystem.GetInvWorldMatrix(gridUid),
            IsPlanet = false,
            ListenerTile = localGridTile,
            ListenerPos = localGridPos,
            Generation = (SoundStageGeneration + 1) % 256,
        };
    }

    private SoundPaths GenerateSoundPaths(SoundStage stage, CancellationToken cancellationToken = default)
    {
        // TODO: what with these "constants"
        var sqrt2 = float.Sqrt(2);
        (Vector2i, float, bool)[] candidateOffsets = [
            (Vector2i.Up, 1, false), (Vector2i.Right, 1, false), (Vector2i.Down, 1, false), (Vector2i.Left, 1, false),
            (Vector2i.UpLeft, sqrt2, true), (Vector2i.UpRight, sqrt2, true), (Vector2i.DownRight, sqrt2, true), (Vector2i.DownLeft, sqrt2, true),
        ];

        // init
        Dictionary<Vector2i, Vector2i?> paths = new();

        var comparer = Comparer<(Vector2i?, float, Vector2i)>.Create((a, b) => -a.Item2.CompareTo(b.Item2));
        PriorityQueue<(Vector2i?, float, Vector2i)> queue = new(32, comparer);

        paths[stage.ListenerTile] = null;
        queue.Add((null, 0f, stage.ListenerTile));

        while (queue.Count > 0)
        {
            var (lastTile, distance, tile) = queue.Take();

            paths[tile] = lastTile;

            // Troll physics: we are actually putting walls into the soundstage, we just don't continue spreading from them
            if (stage.IsPassable(tile) /*&& nextDistance <= 22*/)
            {
                foreach (var (offset, offsetDistance, checkDiagonal) in candidateOffsets)
                {
                    var candidate = tile + offset;
                    var nextDistance = distance + offsetDistance;

                    if (checkDiagonal)
                    {
                        // Make sure both of the cardinals are passable
                        if (!stage.IsPassable(tile + new Vector2i(offset.X, 0)) || !stage.IsPassable(tile + new Vector2i(0, offset.Y)))
                            continue;
                    }

                    // insert the candidate before it's actually processed so it doesn't get queued multiple times
                    // TODO: refactor this to not be so dirty
                    if (paths.TryAdd(candidate, tile))
                        queue.Add((tile, nextDistance, candidate));
                }
            }
        }

        return new SoundPaths
        {
            Stage = stage,
            Paths = paths
        };
    }

    public override void Update(float frameTime)
    {
        if (SoundPathsTask != null)
        {
            if (SoundPathsTask.IsCompleted)
            {
#pragma warning disable RA0004
                CurrentSoundPaths = SoundPathsTask.Result;
#pragma warning restore RA0004
                SoundStageGeneration = CurrentSoundPaths.Stage.Generation;
                LastLocalPos = CurrentSoundPaths.Stage.ListenerPos;
                LastUpdatedTile = CurrentSoundPaths.Stage.ListenerTile;
                LastGridUid = CurrentSoundPaths.Stage.GridUid;
                SoundPathsTask.Dispose();
                SoundPathsTask = null;
            }
            else if (SoundPathsTask.IsFaulted || SoundPathsTask.IsCanceled)
            {
                SoundPathsTask.Dispose();
                CurrentSoundPaths = null;
                SoundPathsTask = null;
            }
            else
            {
                return;
            }
        }

        if (!_dirtySoundStage)
            return;

        // Even if we don't actually gen a new soundstage, we want to wait until we get to make one
        _dirtySoundStage = false;

        var newSoundStage = SnapshotStage();

        if (newSoundStage == null)
        {
            // Soundstage is invalidated
            CurrentSoundPaths = null;
            SoundStageGeneration = (SoundStageGeneration + 1) % 256;
            // TODO: is the following the right thing to do here?
            LastLocalPos = null;
            LastUpdatedTile = null;
            LastGridUid = null;
            return;
        }

        // TODO: Multithread here
        // Update state with new soundstage
        //CurrentSoundPaths = GenerateSoundPaths(newSoundStage);
        SoundPathsTask = Task.Run(() => GenerateSoundPaths(newSoundStage));
    }

    private void OnAirtightInit(Entity<AirtightComponent> ent, ref ComponentInit args)
    {
        // add to grid map
        _dirtySoundStage = true;
    }

    private void OnAirtightShutdown(Entity<AirtightComponent> ent, ref ComponentShutdown args)
    {
        // remove from grid map
        _dirtySoundStage = true;
    }

    private void OnAirtightAnchorChange(Entity<AirtightComponent> ent, ref AnchorStateChangedEvent args)
    {
        // add/remove from grid map
        _dirtySoundStage = true;
    }

    private void OnAirtightReAnchor(Entity<AirtightComponent> ent, ref ReAnchorEvent args)
    {
        // remove from old grid map and add to new grid map
        _dirtySoundStage = true;
    }

    private void OnAirtightMove(Entity<AirtightComponent> ent, ref MoveEvent args)
    {
        _dirtySoundStage = true;
    }

    private void OnAirtightStateChange(Entity<AirtightComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // add/remove depending on whether ent.Comp.AirBlocked is true/false now relative to its status on the grid map
        _dirtySoundStage = true;
    }

    private void OnPlayerMove(Entity<ESLocalPlayerMarkerComponent> ent, ref MoveEvent args)
    {
        var newGrid = _transformSystem.GetGrid(args.NewPosition);

        var newWorldPos = _transformSystem.ToWorldPosition(args.NewPosition);

        // TODO: do some stuff on map change!! Nuke everything!! RAAH
        //if (_transformSystem.GetMapId(args.NewPosition) != _transformSystem.GetMapId(Last))

        if (newGrid != LastGridUid)
        {
            _dirtySoundStage = true;
        }
        else if (!_mapGridQuery.TryGetComponent(LastGridUid, out var grid))
        {
            // dirty soundstage when moving between grids
            _dirtySoundStage = true;
        }

        if (CurrentSoundPaths != null)
        {
            LastLocalPos = CurrentSoundPaths.Stage.WorldToLocal(newWorldPos);
            var newLocalTile = CurrentSoundPaths.Stage.LocalToTile(LastLocalPos.Value);
            if (newLocalTile != LastUpdatedTile)
            {
                // Moved between tiles
                _dirtySoundStage = true;
            }
        }
    }
}
