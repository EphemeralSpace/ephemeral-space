using System.Numerics;
using Content.Client._ES.LocalPlayer;
using Content.Shared.Atmos.Components;
using Content.Shared.Maps;
using Robust.Client.GameObjects;
using Robust.Client.GameStates;
using Robust.Client.Graphics;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using YamlDotNet.Core.Tokens;

namespace Content.Client._ES.SoundOcclusion;

public sealed class TomenoSoundOcclusionSystem : EntitySystem
{
    [Dependency] private readonly IMapManager _mapManager = default!;
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
        - how to get pvs size? current grid?
        - X should we just use a Vector2i dictionary for occlusion and query for all airtight?
    - X Fill boolean tilemap with airtight data
        - X should i just grab all the airtight in pvs and slot it in?
        - X how to handle offgrid? multigrid?
            - X offgrid - space is occluded, planet is not
            - X multigrid - idk, probably just query center of tiles?
    - X make an overlay for this data
    -- entering beautiful engine agnostic algorithm land. the serenity of those drums is astounding
    - X Flood fill from the listener pos
        - X breadth-first
        - X pathfinding data?
            - X coordinates to previous tile?
            - has seen a wall flag for listener room size, count fresh tiles that havent seen a wall
    - X tracing against the tilemap/checking "los" - we use supercover dda
    - pathfinding sounds - semi-done, currently naive
        - check los first
            - check if sound is in listener room? - for skipping premium reverb later
        - X freelo with prev-tile
    - shorten path - X semi-done - doesn't matter for now
        - from either side with sound traces? should be fine to step across the path for now - since we use diagonals, yes
            - maybe binary search or smt later
            - last visible tile from hearer becomes the "portal" - for premium reverb later
                - maybe we can use this rn to cache hearer side traces and save a bit if a path crosses over it
    -- reentering hell
    - when the soundstage becomes dirty (airtights move, etc), mark it as dirty and update on next client tick (Update)
    - when a new soundstage is generated, schedule a background task thread to calculate the new paths
        - just schedule using System.Threading.Tasks? maybe?
        - we can only update the sounds/soundpaths up to 20 times a second - enforce this somehow?
            - maybe we just choke off soundstage updates in general? <- seems like the way
    - this means we will need to snapshot all grid coordinate system info alongside the soundstage?
    - mutex on the soundstage, background thread needs to wait for the latest soundstage to be written incase it gets dirty again
        - either this, or some sort of double-buffering for the soundstage - mutex seems simpler, the bg thread can wait
        - viceversa, the Update skips updating if the background thread is reading
            - the background thread should actually copy the soundstage data and use that maybe?
                - this way the soundstage is unlocked for most of it so it can freely update in the meantime
    - when the background thread is working, the soundpath data must stay available
        - the background thread must thread-safely switch the soundpaths over - maybe this emits an event for sounds to update?
    - planning for sounds - some sort of structure that the sound will get its path in, threadsafe, sound keeps it?
        - the sound will be updating the occlusion ratio based on the distances, updating the start & end point of the path
    */

    public record SoundstagePathTile(float Distance, Vector2i? PreviousTile);

    public Dictionary<Vector2i, bool> Soundstage = new();
    public Dictionary<Vector2i, SoundstagePathTile> SoundstagePaths = new();
    public Vector2i? LocalGridTile = null;
    public Vector2? LocalGridPos = null;
    public EntityUid? LocalGridUid = null;

    public bool IsSoundstageValid()
    {
        return (LocalGridTile.HasValue && LocalGridPos.HasValue && LocalGridUid.HasValue);
    }

    public bool IsSoundPassable(Vector2i tile)
    {
        // TODO: For planet maps, lack of value is true instead
        return Soundstage.TryGetValue(tile, out var soundable) && soundable;
    }
    public void SetupSoundstage()
    {
        Soundstage.Clear();
        SoundstagePaths.Clear();
        LocalGridTile = null;
        LocalGridPos = null;
        LocalGridUid = null;

        // Get local player
        var localQuery = EntityQueryEnumerator<ESLocalPlayerMarkerComponent>();
        if (!localQuery.MoveNext(out var localUid, out _))
            return;
        localQuery.Dispose();

        if (!_transformQuery.TryGetComponent(localUid, out var transform))
            return;

        EntityUid gridUid;
        if (transform.GridUid == null)
        {
            // TODO: Find the nearest grid? Only matters on planets tho
            //var mapUid = transform.MapUid;
            return;
        }
        else
        {
            gridUid = (EntityUid) transform.GridUid;
        }

        if (!_mapGridQuery.TryGetComponent(gridUid, out var grid))
            return;

        LocalGridTile = _sharedMapSystem.TileIndicesFor(gridUid, grid, transform.Coordinates);
        LocalGridPos = _sharedMapSystem.LocalToGrid(gridUid, grid, transform.Coordinates);
        LocalGridUid = gridUid;

        if (!LocalGridTile.HasValue || !LocalGridPos.HasValue)
            return;

        var soundRange = 15;
        var tileRange = Box2.CenteredAround((Vector2)LocalGridPos, new Vector2(1 + soundRange * 2, 1 + soundRange * 2));

        var tilesEnumerator = _sharedMapSystem.GetLocalTilesEnumerator(gridUid, grid, tileRange, true);

        while (tilesEnumerator.MoveNext(out var tile))
        {
            if (!_turfSystem.IsSpace(tile.Tile))
                Soundstage[tile.GridIndices] = true;
        }

        var airtightQuery = EntityQueryEnumerator<AirtightComponent>();
        while (airtightQuery.MoveNext(out var uid, out var comp))
        {
            if (!comp.AirBlocked)
                continue;

            if (!_transformQuery.TryGetComponent(uid, out var thisTransform))
                continue;

            // TODO: Can these be offgrid? How does planet stuff work?

            var transformedTile = _sharedMapSystem.LocalToTile(gridUid, grid, thisTransform.Coordinates);
            Soundstage[transformedTile] = false;
        }

        // Ok babey time to get to da flodd filler

        var sqrt2 = float.Sqrt(2);
        (Vector2i, float, bool)[] candidateOffsets = [
            (Vector2i.Up, 1, false), (Vector2i.Right, 1, false), (Vector2i.Down, 1, false), (Vector2i.Left, 1, false),
            (Vector2i.UpLeft, sqrt2, true), (Vector2i.UpRight, sqrt2, true), (Vector2i.DownRight, sqrt2, true), (Vector2i.DownLeft, sqrt2, true),
        ];

        Queue<(Vector2i?, float, Vector2i)> queue = new();
        queue.Enqueue((null, 0f, LocalGridTile.Value));
        while (queue.Count > 0)
        {
            var (lastTile, distance, tile) = queue.Dequeue();
            //if (Airtights.TryGetValue(tile, out var airtight))

            // wtf? why is this happening?
            if (SoundstagePaths.ContainsKey(tile))
                continue;

            SoundstagePathTile myStage = new(distance, lastTile);
            SoundstagePaths[tile] = myStage;
            //Vector2i[] candidates = [tile + Vector2i.Up, tile + Vector2i.Right, tile + Vector2i.Down, tile + Vector2i.Left, tile + Vector2i.UpLeft, tile + Vector2i.UpRight, tile + Vector2i.DownRight, tile + Vector2i.DownLeft,];

            // Troll physics: we are actually putting walls into the soundstage, we just don't continue spreading from them
            if (IsSoundPassable(tile) /*&& nextDistance <= 22*/)
            {
                foreach (var (offset, offsetDistance, checkDiagonal) in candidateOffsets)
                {
                    var candidate = tile + offset;
                    var nextDistance = distance + offsetDistance;
                    if (candidate == lastTile)
                        continue;

                    if (checkDiagonal)
                    {
                        // Make sure both of the cardinals are passable
                        if (!IsSoundPassable(tile + new Vector2i(offset.X, 0)) || !IsSoundPassable(tile + new Vector2i(0, offset.Y)))
                            continue;
                    }

                    // i don't think this is possible in dfs, but i'll keep it in here until i test
                    /*if (SoundstagePaths.TryGetValue(candidate, out var other))
                    {
                        if (other.Distance <= nextDistance)
                            continue;
                    }*/

                    if (SoundstagePaths.ContainsKey(candidate))
                        continue;

                    queue.Enqueue((tile, nextDistance, candidate));
                }
            }
        }
    }

    public class SoundstagePath
    {
        private float Distance { get; set; }
        private float PathDistance { get; set; }

        private Vector2i EmitterPortal { get; set; }
        private Vector2i ListenerPortal { get; set; }

        private List<Vector2i> PathTiles;
        private List<Vector2> PathPoints;
        public SoundstagePath()
        {
            PathTiles = new List<Vector2i>();
            PathPoints = new List<Vector2>();
        }
    }

    // Supercover DDA Algorithm
    public bool CheckVisibility(Vector2 v1, Vector2 v2)
    {
        // var tileSize = 1f; // TODO: Properly cache grid data so we can do coordinate translations without comps

        var t1 = v1.Floored();

        var dX = v2.X - v1.X;
        var dY = v2.Y - v1.Y;

        var sX = dX > 0 ? 1 : -1;
        var sY = dY > 0 ? 1 : -1;

        var tDeltaX = dX != 0 ? Math.Abs(1f / dX) : float.PositiveInfinity;
        var tDeltaY = dY != 0 ? Math.Abs(1f / dY) : float.PositiveInfinity;

        var tMaxX = dX > 0 ? ((t1.X + 1 - v1.X) / dX) : (dX < 0 ? (t1.X - v1.X) / dX : float.PositiveInfinity);
        var tMaxY = dY > 0 ? ((t1.Y + 1 - v1.Y) / dY) : (dY < 0 ? (t1.Y - v1.Y) / dY : float.PositiveInfinity);

        // If this was the proper algo then here you would check the start tile, but I think it's better if we don't
        //   for the sake of sound emitters within walls

        var steps = Math.Abs(Math.Floor(v2.X) - t1.X) + Math.Abs(Math.Floor(v2.Y) - t1.Y);
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
            if (!IsSoundPassable(t1))
            {
                return false;
            }
        }

        return true;
    }

    public (bool, float, float, List<Vector2i>?, List<Vector2>?) FindSoundPath(Vector2 pos, Vector2i tile)
    {
        if (!IsSoundstageValid() || LocalGridPos == null)
            return (false, 0, 0, null, null);

        if (!SoundstagePaths.TryGetValue(tile, out var startTile))
            return (false, 0, 0, null, null);

        var pathTiles = new List<Vector2i>(); // TODO: We can actually initialize this with the exact capacity

        var curTile = tile;
        while (SoundstagePaths.TryGetValue(curTile, out var pathTile))
        {
            pathTiles.Add(curTile);

            if (pathTile.PreviousTile == null)
                break;

            curTile = pathTile.PreviousTile.Value;
        }

        var pathPoints = new List<Vector2>();
        pathPoints.Add(pos);

        // Time to find the "portal" for the emitter and then the listener
        var emitterPortalPos = 0;
        for (var i = 1; i < pathTiles.Count; i++)
        {
            var nextTile = pathTiles[i];
            if (CheckVisibility(nextTile + Vector2.One / 2, pos))
            {
                emitterPortalPos = i;
            }
        }

        var listenerPortalPos = pathTiles.Count - 1;
        for (var i = pathTiles.Count - 1; i >= emitterPortalPos; i--)
        {
            var nextTile = pathTiles[i];
            if (CheckVisibility(nextTile + Vector2.One / 2, LocalGridPos.Value))
            {
                listenerPortalPos = i;
            }
        }

        for (var i = emitterPortalPos; i <= listenerPortalPos; i++)
        {
            pathPoints.Add(pathTiles[i] + Vector2.One / 2);
        }

        pathPoints.Add(LocalGridPos.Value);

        var rawPathDistance = pathTiles.Count;
        float finalDistance = 0;
        var lastPoint = pos;
        foreach (var pathPoint in pathPoints)
        {
            finalDistance += Vector2.Distance(lastPoint, pathPoint);
        }

        return (true, finalDistance, rawPathDistance, pathTiles, pathPoints);
    }

    private void OnAirtightInit(Entity<AirtightComponent> ent, ref ComponentInit args)
    {
        // add to grid map
        SetupSoundstage();
    }

    private void OnAirtightShutdown(Entity<AirtightComponent> ent, ref ComponentShutdown args)
    {
        // remove from grid map
        SetupSoundstage();
    }

    private void OnAirtightAnchorChange(Entity<AirtightComponent> ent, ref AnchorStateChangedEvent args)
    {
        // add/remove from grid map
        SetupSoundstage();
    }

    private void OnAirtightReAnchor(Entity<AirtightComponent> ent, ref ReAnchorEvent args)
    {
        // remove from old grid map and add to new grid map
        SetupSoundstage();
    }

    private void OnAirtightMove(Entity<AirtightComponent> ent, ref MoveEvent args)
    {
        // you get the idea
        SetupSoundstage();
    }

    private void OnAirtightStateChange(Entity<AirtightComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        // add/remove depending on whether ent.Comp.AirBlocked is true/false now relative to its status on the grid map
        // you get the idea
        SetupSoundstage();
    }

    private void OnPlayerMove(Entity<ESLocalPlayerMarkerComponent> ent, ref MoveEvent args)
    {
        if (!LocalGridPos.HasValue || !_mapGridQuery.TryGetComponent(LocalGridUid, out var grid))
        {
            SetupSoundstage();
            return;
        }

        LocalGridPos = _sharedMapSystem.LocalToGrid(LocalGridUid.Value, grid, args.NewPosition);
        var newGridTile = _sharedMapSystem.TileIndicesFor(LocalGridUid.Value, grid, args.NewPosition);

        if (newGridTile != LocalGridTile)
        {
            SetupSoundstage();
        }
    }
}
