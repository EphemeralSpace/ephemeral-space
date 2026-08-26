using System.Numerics;
using System.Threading;
using System.Threading.Tasks;
using Content.Client._ES.LocalPlayer;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Content.Shared.Wall;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Client.Timing;
using Robust.Shared.Audio.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Utility;
using YamlDotNet.Core.Tokens;

namespace Content.Client._ES.SoundOcclusion;

public sealed partial class TomenoSoundOcclusionSystem : EntitySystem
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IOverlayManager _overlayManager = default!;
    // Prediction checks, realtime update choking
    [Dependency] private IClientGameTiming _gameTiming = default!;

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
    }

    public int SoundStageGeneration = 0;

    // Used for tracking significant movement updates
    public EntityUid? LastGridUid = null;
    // Used for updating paths
    public Vector2? LastLocalPos = null;

    public SoundPaths? CurrentSoundPaths = null;

    private bool _dirtySoundStage = true;

    private Task<SoundPaths>? _soundPathsTask = null;
    private long _lastUpdateTicks = 0;

    private const long UpdateIntervalTicks = TimeSpan.TicksPerMillisecond * 50;

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

        //
        public required int Generation { get; init; }

        /// <summary>
        /// Returns the passability value of a stage tile. True for tiles that are passable, false for impassable.
        /// Passability of tiles that haven't been recorded is decided based on map type.
        ///     Planets are passable, space is impassable.
        /// </summary>
        public bool IsPassable(Vector2i position)
        {
            return Passable.GetValueOrDefault(position, IsPlanet);
        }

        /// <summary>
        /// Converts world coordinates to stage coordinates.
        /// </summary>
        public Vector2 WorldToStage(Vector2 posWorld)
        {
            return Vector2.Transform(posWorld, InvWorldMatrix);
        }

        /// <summary>
        /// Converts stage coordinates to world coordinates.
        /// </summary>
        public Vector2 StageToWorld(Vector2 posStage)
        {
            return Vector2.Transform(posStage, WorldMatrix);
        }

        /// <summary>
        /// Converts stage coordinates to a stage tile.
        /// </summary>
        public Vector2i StageToTile(Vector2 posStage)
        {
            // honestly TileSize is a no-op 100% of the time right now (AFAIK)
            var x = (int)Math.Floor(posStage.X / TileSize);
            var y = (int)Math.Floor(posStage.Y / TileSize);
            return new Vector2i(x, y);
        }

        /// <summary>
        /// Converts world coordinates to a stage tile.
        /// </summary>
        public Vector2i WorldToTile(Vector2 posWorld)
        {
            return StageToTile(WorldToStage(posWorld));
        }

        /// <summary>
        /// Checks for sound-visibility on the soundstage using "Supercover DDA".
        /// </summary>
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

            if (!IsPassable(t2))
                return false;

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
        // Portal locations - if they're null, that means we have a direct path.
        public Vector2? EmitterPortal { get; init; }
        public Vector2? ListenerPortal { get; init; }

        // Pathed distance between Emitter and Listener portals
        public float PortalDistance { get; init; }

        // For invalidating the path
        public int Generation { get; init; }
        public Vector2i EmitterTile { get; init; }
    }

    public sealed class SoundPaths
    {
        public required SoundStage Stage { get; init; }
        public required Dictionary<Vector2i, Vector2i?> Paths { get; init; }
    }

    /// <summary>
    /// Finds a path from an emitter position to the listener in the current active SoundPaths.
    /// If a path is not found, returns null.
    /// If a direct path is found, returns a PathResult with no portals.
    /// If a path is found, returns a PathResult.
    /// </summary>
    public PathResult? FindPath(Vector2 emitter)
    {
        if (CurrentSoundPaths == null || !LastLocalPos.HasValue)
            return null;

        var emitterTile = CurrentSoundPaths.Stage.StageToTile(emitter);

        if (!CurrentSoundPaths.Paths.ContainsKey(emitterTile))
            return null;

        if (CurrentSoundPaths.Stage.CheckVisibility(emitter, LastLocalPos.Value))
        {
            return new PathResult
            {
                EmitterPortal = null,
                ListenerPortal = null,
                PortalDistance = 0,
                Generation = CurrentSoundPaths.Stage.Generation,
                EmitterTile = emitterTile,
            };
        }

        var pathTiles = new List<Vector2i>();

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

        if (pathTiles.Count <= 2)
        {
            return new PathResult
            {
                EmitterPortal = null,
                ListenerPortal = null,
                PortalDistance = 0,
                Generation = CurrentSoundPaths.Stage.Generation,
                EmitterTile = emitterTile,
            };
        }

        // Time to find the "portals" for the listener and then the emitter
        var emitterPortalPos = 0;
        var listenerPortalPos = pathTiles.Count - 1;

        for (var i = listenerPortalPos; i >= emitterPortalPos; i--)
        {
            var nextTile = pathTiles[i];
            if (CurrentSoundPaths.Stage.CheckVisibility(nextTile + Vector2.One / 2, CurrentSoundPaths.Stage.StageToTile(LastLocalPos.Value) + Vector2.One / 2))
            {
                listenerPortalPos = i;
            }
        }

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
            EmitterPortal = pathTiles[emitterPortalPos] + new Vector2(0.5f, 0.5f),
            ListenerPortal = pathTiles[listenerPortalPos] + new Vector2(0.5f, 0.5f),
            PortalDistance = portalDistance,
            Generation = CurrentSoundPaths.Stage.Generation,
            EmitterTile = emitterTile,
        };
    }

    private readonly Dictionary<AudioComponent, PathResult> _pathCache = new();

    /// <summary>
    /// Finds a path from an entity to the listener in the current active SoundPaths.
    /// The position will be modified based on the components present on the entity.
    /// The path may be cached, keyed to an AudioComponent, if it is passed.
    /// If a path is not found, returns null.
    /// If a direct path is found, returns a PathResult with no portals.
    /// If a path is found, returns a PathResult.
    /// </summary>
    public PathResult? FindEntityPath(EntityUid entity, Vector2? position, AudioComponent? audio)
    {
        if (CurrentSoundPaths == null || !LastLocalPos.HasValue)
            return null;

        if (!_entityManager.TryGetComponent<TransformComponent>(entity, out var transformComponent))
            return null;

        var emitter = position ?? CurrentSoundPaths.Stage.WorldToStage(_transformSystem.GetWorldPosition(transformComponent));
        var emitterTile = CurrentSoundPaths.Stage.StageToTile(emitter);

        // No path found in cache / Found path is invalid (out of date)
        if (audio != null && _pathCache.TryGetValue(audio, out var path))
        {
            if (IsPathValid(path, emitterTile))
                return path;

            _pathCache.Remove(audio);
        }

        if (!CurrentSoundPaths.Paths.ContainsKey(emitterTile))
            return null;

        // For sounds coming from inside occluders:
        // center of the tile:
        //   wallmount: tile the wallmount faces
        //   else: follow soundpath
        // else: nearest unoccluded tile
        if (!CurrentSoundPaths.Stage.IsPassable(emitterTile))
        {
            var emitterCenterDelta = (emitterTile + Vector2.One / 2) - emitter;
            var newEmitterTile = CurrentSoundPaths.Paths[emitterTile];
            // var newEmitterPos = emitter;

            if (newEmitterTile != null)
            {
                if (emitterCenterDelta.X >= -0.1f && emitterCenterDelta.X <= 0.1f
                    && emitterCenterDelta.Y >= -0.1f && emitterCenterDelta.Y <= 0.1f)
                {
                    var grid = _transformSystem.GetGrid(entity);
                    if (transformComponent.ParentUid.IsValid() && _entityManager.TryGetComponent<WallMountComponent>(transformComponent.ParentUid, out var wallMount))
                    {
                        if (grid == null)
                            return null;

                        var (_, emitterRotation) = _transformSystem.GetRelativePositionRotation(transformComponent, grid.Value);
                        emitterRotation += wallMount.Direction;
                        var emitterRotVector = emitterRotation.ToWorldVec();

                        if (Math.Abs(emitterRotVector.X) > Math.Abs(emitterRotVector.Y))
                            emitterTile += emitterRotVector.X > 0 ? Vector2i.Right : Vector2i.Left;
                        else
                            emitterTile += emitterRotVector.Y > 0 ? Vector2i.Up : Vector2i.Down;
                    }
                    else
                    {
                        emitterTile = newEmitterTile.Value;
                    }
                }
                else
                {
                    var bestDistance = float.MaxValue;
                    Func<Vector2i, bool> checkCandidate = (candidateDir) =>
                    {
                        var candidate = emitterTile + candidateDir;
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
                    checkCandidate(Vector2i.Up);
                    checkCandidate(Vector2i.Right);
                    checkCandidate(Vector2i.Down);
                    checkCandidate(Vector2i.Left);

                    if (!CurrentSoundPaths.Paths.ContainsKey(newEmitterTile.Value))
                        return null;

                    emitterTile = newEmitterTile.Value;
                }
                // Offset the sound pos into the neighboring tile to make sure that it's always traceable
                if (emitterTile.X > emitter.X)
                    emitter.X = emitterTile.X + 0.0001f;
                if (emitterTile.Y > emitter.Y)
                    emitter.Y = emitterTile.Y + 0.0001f;
                if (emitterTile.X < emitter.X)
                    emitter.X = emitterTile.X + 0.9999f;
                if (emitterTile.Y < emitter.Y)
                    emitter.Y = emitterTile.Y + 0.9999f;
            }
        }

        var result = FindPath(emitter);

        if (audio != null && result != null)
            _pathCache.Add(audio, result);

        return result;
    }

    /// <summary>
    /// Records the nearby tiles and Airtight entities into a new SoundStage object.
    /// </summary>
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

        var soundRange = 15;
        var tileRange = Box2.CenteredAround((Vector2)localGridPos, new Vector2(1 + soundRange * 2, 1 + soundRange * 2));

        // TODO: this is obsolete
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

    /// <summary>
    /// Checks whether a PathResult is up-to-date and can be reused.
    /// </summary>
    public bool IsPathValid(PathResult path, Vector2 emitterPos)
    {
        if (CurrentSoundPaths == null)
            return false;

        if (path.Generation != CurrentSoundPaths.Stage.Generation)
            return false;

        if (path.EmitterTile != emitterPos.Floored())
            return false;

        return true;
    }

    /// <summary>
    /// Generate new SoundPaths for the passed SoundStage.
    /// </summary>
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

            // We are actually pathing into walls, we just don't continue pathing from them
            if (stage.IsPassable(tile) || lastTile == null) // Actually continue if we are starting from inside a wall/airlock...
            {
                foreach (var (offset, offsetDistance, checkDiagonal) in candidateOffsets)
                {
                    var candidate = tile + offset;
                    var nextDistance = distance + offsetDistance;

                    if (checkDiagonal)
                    {
                        // Make sure both of the cardinals are passable for diagonals
                        if (!stage.IsPassable(tile + new Vector2i(offset.X, 0)) || !stage.IsPassable(tile + new Vector2i(0, offset.Y)))
                            continue;
                    }

                    // insert the candidate before it's actually processed so it doesn't get queued multiple times
                    // TODO: refactor this to not be so dirty?
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
        if (_soundPathsTask != null)
        {
            if (_soundPathsTask.IsCompleted)
            {
#pragma warning disable RA0004
                CurrentSoundPaths = _soundPathsTask.Result;
#pragma warning restore RA0004
                SoundStageGeneration = CurrentSoundPaths.Stage.Generation;
                LastLocalPos = CurrentSoundPaths.Stage.ListenerPos;
                LastGridUid = CurrentSoundPaths.Stage.GridUid;
                _pathCache.Clear();
                _soundPathsTask.Dispose();
                _soundPathsTask = null;
            }
            else if (_soundPathsTask.IsFaulted || _soundPathsTask.IsCanceled)
            {
                _soundPathsTask.Dispose();
                CurrentSoundPaths = null;
                _soundPathsTask = null;
            }
            else
            {
                return;
            }
        }

        if (!_dirtySoundStage)
            return;

        // Prediction duct tape
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        if (_gameTiming.ServerTime.Ticks < _lastUpdateTicks + UpdateIntervalTicks)
            return;

        // Even if we don't actually gen a new soundstage, we want to wait until we get to make one
        _dirtySoundStage = false;
        _lastUpdateTicks = _gameTiming.ServerTime.Ticks;

        var newSoundStage = SnapshotStage();

        if (newSoundStage == null)
        {
            // Soundstage is invalidated
            CurrentSoundPaths = null;
            SoundStageGeneration = (SoundStageGeneration + 1) % 256;
            // is the following the right thing to do here?
            LastLocalPos = null;
            LastGridUid = null;
            return;
        }

        _soundPathsTask = Task.Run(() => GenerateSoundPaths(newSoundStage));
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
        // Prediction duct tape
        if (!_gameTiming.IsFirstTimePredicted)
            return;

        var newGrid = _transformSystem.GetGrid(args.NewPosition);


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

        if (CurrentSoundPaths != null && LastLocalPos != null)
        {
            var newWorldPos = _transformSystem.ToWorldPosition(args.NewPosition);
            var lastTile = CurrentSoundPaths.Stage.StageToTile(LastLocalPos.Value);
            LastLocalPos = CurrentSoundPaths.Stage.WorldToStage(newWorldPos);
            if (CurrentSoundPaths.Stage.StageToTile(LastLocalPos.Value) != lastTile)
            {
                // Moved between tiles
                _dirtySoundStage = true;
            }
        }
    }
}
