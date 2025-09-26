using System.Diagnostics.CodeAnalysis;
using System.Numerics;
using Content.Shared._ES.SpawnRegion.Components;
using Content.Shared.Examine;
using Content.Shared.Ghost;
using Content.Shared.Maps;
using Content.Shared.Mobs.Systems;
using Content.Shared.Physics;
using Content.Shared.Station.Components;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Physics.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.SpawnRegion;

public abstract class ESSharedSpawnRegionSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly ExamineSystemShared _examine = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    private readonly List<Entity<ESSpawnRegionMarkerComponent>> _markers = new();
    private readonly HashSet<EntityUid> _lookupSet = new();
    private readonly HashSet<Entity<ActorComponent>> _actors = new();

    private EntityQuery<GhostComponent> _ghostQuery;
    private EntityQuery<MapGridComponent> _gridQuery;
    private EntityQuery<PhysicsComponent> _bodyQuery;

    public const int RandomAttempts = 100;
    public const float PlayerViewRadius = 7.5f * 1.4142f; // Account for diagonal

    public override void Initialize()
    {
        base.Initialize();

        _ghostQuery = GetEntityQuery<GhostComponent>();
        _gridQuery = GetEntityQuery<MapGridComponent>();
        _bodyQuery = GetEntityQuery<PhysicsComponent>();
    }

    /// <summary>
    /// Selects a random coordinate inside a given area, filtering primarily by station.
    /// </summary>
    /// <param name="region">The Spawn Region prototype ID used for generally filtering areas.</param>
    /// <param name="station">The station that the area must be on</param>
    /// <param name="outCoords">The randomly selected coordinate. May be null</param>
    /// <param name="blockLayer"><see cref="CollisionGroup"/> used for determining if a given coordinate is "blocked"</param>
    /// <param name="checkPlayerLOS">If true, a coordinate being in player Line of Sight will invalidate it</param>
    /// <param name="minPlayerDistance">Minimum distance from players that a point must be to be valid</param>
    /// <param name="checkAtmosPressure">If true, unsafe atmospheric pressure will invalidate a coordinate</param>
    /// <param name="checkAtmosTemperature">If true, unsafe atmospheric temperature will invalidate a coordinate</param>
    /// <param name="pred">Generic predicate for determining if a coordinate is valid</param>
    /// <returns>If <see cref="outCoords"/> was successfully found in a reasonable amount of time.</returns>
    public bool TryGetRandomAreaCoords(ProtoId<ESSpawnRegionPrototype> region,
        Entity<StationDataComponent?> station,
        [NotNullWhen(true)] out EntityCoordinates? outCoords,
        CollisionGroup blockLayer = CollisionGroup.MobMask | CollisionGroup.Opaque,
        bool checkPlayerLOS = true,
        float minPlayerDistance = 3.5f,
        bool checkAtmosPressure = true,
        bool checkAtmosTemperature = true,
        Func<Entity<TransformComponent>, bool>? pred = null
        )
    {
        outCoords = null;
        if (!Resolve(station, ref station.Comp))
            return false;

        return TryGetRandomAreaCoords(region,
            station.Comp.Grids,
            out outCoords,
            blockLayer,
            checkPlayerLOS,
            minPlayerDistance,
            checkAtmosPressure,
            checkAtmosTemperature,
            pred);
    }

    /// <summary>
    /// Selects a random coordinate inside a given area, filtering primarily by grid
    /// </summary>
    /// <param name="region">The Spawn Region prototype ID used for generally filtering areas.</param>
    /// <param name="gridSet">A set of grids that the area must be located on</param>
    /// <param name="outCoords">The randomly selected coordinate. May be null</param>
    /// <param name="blockLayer"><see cref="CollisionGroup"/> used for determining if a given coordinate is "blocked"</param>
    /// <param name="checkPlayerLOS">If true, a coordinate being in player Line of Sight will invalidate it</param>
    /// <param name="minPlayerDistance">Minimum distance from players that a point must be to be valid</param>
    /// <param name="checkAtmosPressure">If true, unsafe atmospheric pressure will invalidate a coordinate</param>
    /// <param name="checkAtmosTemperature">If true, unsafe atmospheric temperature will invalidate a coordinate</param>
    /// <param name="pred">Generic predicate for determining if a coordinate is valid</param>
    /// <returns>If <see cref="outCoords"/> was successfully found in a reasonable amount of time.</returns>
    public bool TryGetRandomAreaCoords(ProtoId<ESSpawnRegionPrototype> region,
        HashSet<EntityUid> gridSet,
        [NotNullWhen(true)] out EntityCoordinates? outCoords,
        CollisionGroup blockLayer = CollisionGroup.MobMask | CollisionGroup.Opaque,
        bool checkPlayerLOS = true,
        float minPlayerDistance = 3.5f,
        bool checkAtmosPressure = true,
        bool checkAtmosTemperature = true,
        Func<Entity<TransformComponent>, bool>? pred = null
        )
    {
        outCoords = null;

        _markers.Clear();
        var query = EntityQueryEnumerator<ESSpawnRegionMarkerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            _markers.Add((uid, comp));
        }

        var attempts = Math.Min(RandomAttempts, _markers.Count);
        for (var i = 0; i < attempts; i++)
        {
            var marker = _random.PickAndTake(_markers);
            if (!IsMarkerValid(marker))
                continue;

            outCoords = Transform(marker).Coordinates;
            return true;
        }
        return false;

        bool IsMarkerValid(Entity<ESSpawnRegionMarkerComponent> ent)
        {
            if (ent.Comp.Area != region)
                return false;

            var xform = Transform(ent);
            var mapId = xform.MapID;

            if (!xform.Anchored ||
                _transform.GetGrid((ent, xform)) is not { } grid ||
                !gridSet.Contains(grid) ||
                !_gridQuery.TryComp(grid, out var gridComp))
                return false;

            if (pred != null)
            {
                if (!pred.Invoke((ent, xform)))
                    return false;
            }

            var gridIndices = _transform.GetGridOrMapTilePosition(ent, xform);
            var tileRef = _map.GetTileRef((grid, gridComp), gridIndices);

            _lookupSet.Clear();
            _entityLookup.GetEntitiesInTile(tileRef, _lookupSet, LookupFlags.Dynamic | LookupFlags.Static);
            foreach (var lookupEnt in _lookupSet)
            {
                if (_bodyQuery.TryComp(lookupEnt, out var body) &&
                    body.Hard &&
                    (body.CollisionMask & (int) blockLayer) != 0)
                    return false;
            }

            if (checkPlayerLOS)
            {
                _actors.Clear();
                var box = Box2.CenteredAround(_transform.GetWorldPosition(xform), PlayerViewRadius * Vector2.One * 2);
                _entityLookup.GetEntitiesIntersecting(mapId, box, _actors, LookupFlags.Dynamic | LookupFlags.Static);
                foreach (var actor in _actors)
                {
                    if (_ghostQuery.HasComp(actor) || !_mobState.IsAlive(actor) || !_mobState.IsCritical(actor))
                        continue;

                    if (_examine.InRangeUnOccluded(ent, actor.Owner))
                        return false;
                }
            }

            if (minPlayerDistance > 0.0f)
            {
                _actors.Clear();
                _entityLookup.GetEntitiesInRange(xform.Coordinates, minPlayerDistance, _actors);
                foreach (var actor in _actors)
                {
                    if (_ghostQuery.HasComp(actor) || !_mobState.IsAlive(actor) || !_mobState.IsCritical(actor))
                        continue;

                    return false;
                }
            }

            if (checkAtmosPressure)
            {
                if (!IsMarkerPressureSafe((ent, ent, xform)))
                    return false;
            }

            if (checkAtmosTemperature)
            {
                if (!IsMarkerTemperatureSafe((ent, ent, xform)))
                    return false;
            }

            return true;
        }
    }

    protected virtual bool IsMarkerPressureSafe(Entity<ESSpawnRegionMarkerComponent, TransformComponent> ent)
    {
        return true;
    }

    protected virtual bool IsMarkerTemperatureSafe(Entity<ESSpawnRegionMarkerComponent, TransformComponent> ent)
    {
        return true;
    }
}
