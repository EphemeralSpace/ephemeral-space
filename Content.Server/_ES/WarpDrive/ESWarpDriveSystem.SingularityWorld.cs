using System.Linq;
using Content.Server._ES.WarpDrive.Components;
using Content.Server.Popups;
using Content.Server.Station.Systems;
using Content.Shared._ES.SpawnRegion;
using Content.Shared._ES.SpawnRegion.Components;
using Content.Shared._Offbrand.Wounds;
using Content.Shared.Ghost;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._ES.WarpDrive;

public sealed partial class ESWarpDriveSystem
{
    [Dependency] private MapLoaderSystem _loader = default!;
    [Dependency] private LinkedEntitySystem _linked = default!;
    [Dependency] private PopupSystem _popup = default!;
    [Dependency] private ESSharedSpawnRegionSystem _spawnRegion = default!;
    [Dependency] private StationSystem _station = default!;
    [Dependency] private BrainDamageSystem _brainDamage = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private SharedTransformSystem _transform = default!;

    private static readonly EntProtoId TeleportEffect = "ESTeleportEffectWarpDrive";
    private static readonly ProtoId<ESSpawnRegionPrototype> TeleportStation = "ESSingularityWorldTeleportStation";
    private static readonly ProtoId<ESSpawnRegionPrototype> TeleportInWorld = "ESSingularityWorldTeleportInWorld";

    public MapId? SingularityWorldMapId;
    public HashSet<EntityUid>? SingularityWorldGrids;

    private void InitializeSingularityWorld()
    {
        SubscribeLocalEvent<ESWarpDriveComponent, PortalTeleportedEvent>(OnWarpDriveTeleport);
    }

    private void OnWarpDriveTeleport(Entity<ESWarpDriveComponent> ent, ref PortalTeleportedEvent args)
    {
        TryTeleportToWarp(ent.Comp.SingularityWorldTeleportOutTime, args.Entity);
    }

    private void ActiveTickSingularityWorld()
    {
        var query = EntityQueryEnumerator<ESSingularityWorldTeleportedEntityComponent, TransformComponent>();

        while (query.MoveNext(out var uid, out var time, out var xform))
        {
            if (_timing.CurTime < time.TeleportOutTime)
                continue;

            // choose area to send them to
            var station = _station.GetStationsSet().First(); // forgive me
            if (!_spawnRegion.TryGetRandomCoordsInRegion(TeleportStation,
                    station,
                    out var coords,
                    checkPlayerLOS: false,
                    minPlayerDistance: 1f))
            {
                Log.Error($"Couldn't send entity {uid} anywhere after teleport timer is up somehow !!! They are fucking scared!!!");
                RemCompDeferred<ESSingularityWorldTeleportedEntityComponent>(uid);
                continue;
            }

            SpawnAtPosition(TeleportEffect, xform.Coordinates);
            SpawnAtPosition(TeleportEffect, coords.Value);
            _transform.SetCoordinates(uid, coords.Value);
            RemCompDeferred<ESSingularityWorldTeleportedEntityComponent>(uid);
        }
    }

    private void StartedSingularityWorld(ESWarpDriveGameRuleComponent component)
    {
        // Load singularity world
        var opts = DeserializationOptions.Default with { InitializeMaps = true };
        if (!_loader.TryLoadMap(component.SingularityWorldMap, out var map, out var grids, opts))
        {
            throw new Exception($"Failed to load singularity world map {component.SingularityWorldMap}");
        }

        SingularityWorldMapId = map.Value.Comp.MapId;
        SingularityWorldGrids = grids.Select(a => a.Owner).ToHashSet();
        Log.Info($"Created new singularity world at map ID {SingularityWorldMapId}");

        // Properly set up the teleporting effect
        var query = AllEntityQuery<ESWarpDriveComponent>();
        while (query.MoveNext(out var driveEntity, out _))
        {
            EntityUid? teleportLocation = null;
            // Pick a random in-world marker to be the teleport location
            // (ideally itd pick any but whatever not rn)
            var locationQuery = EntityQueryEnumerator<ESSpawnRegionMarkerComponent>();
            while (locationQuery.MoveNext(out var regionEntity, out var marker))
            {
                if (marker.Area != TeleportInWorld)
                    continue;

                teleportLocation = regionEntity;
                break;
            }

            if (teleportLocation == null)
            {
                throw new Exception("Singularity world map has no valid teleport locations for linking!");
            }

            // Set up link
            _linked.TryLink(driveEntity, teleportLocation.Value);
        }
    }

    public void TryTeleportToWarp(TimeSpan teleportOutTime, EntityUid ent)
    {
        if (HasComp<GhostComponent>(ent))
            return;

        var teleport = EnsureComp<ESSingularityWorldTeleportedEntityComponent>(ent);
        teleport.TeleportOutTime = _timing.CurTime + teleportOutTime;

        SpawnAtPosition(TeleportEffect, Transform(ent).Coordinates);
        _popup.PopupEntity(Loc.GetString("es-warp-drive-singularity-teleport-user"), ent, ent);
        IncrementTeleportedEntitiesCount();
        _brainDamage.TryChangeBrainDamage(ent, 20);
    }

    public HashSet<EntityUid>? GetSingularityWorldGrids()
    {

        return SingularityWorldGrids;
    }
}
