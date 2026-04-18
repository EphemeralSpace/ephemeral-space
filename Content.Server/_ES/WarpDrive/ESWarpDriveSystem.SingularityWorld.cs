using Content.Server._ES.WarpDrive.Components;
using Content.Shared._ES.SpawnRegion;
using Content.Shared._ES.SpawnRegion.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Teleportation.Components;
using Content.Shared.Teleportation.Systems;
using Robust.Shared.Audio;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.WarpDrive;

public sealed partial class ESWarpDriveSystem
{
    [Dependency] private readonly MapLoaderSystem _loader = default!;
    [Dependency] private readonly LinkedEntitySystem _linked = default!;

    private static readonly ProtoId<ESSpawnRegionPrototype> SingularityWorldTeleportStation = "ESSingularityWorldTeleportStation";
    private static readonly ProtoId<ESSpawnRegionPrototype> SingularityWorldTeleportInWorld = "ESSingularityWorldTeleportInWorld";

    public MapId? SingularityWorldMapId;

    private void InitializeSingularityWorld()
    {
    }

    private void StartedSingularityWorld(ESWarpDriveGameRuleComponent component)
    {
        // Load singularity world
        var opts = DeserializationOptions.Default with {InitializeMaps = true};
        if (!_loader.TryLoadMap(component.SingularityWorldMap, out var map, out _, opts))
        {
            throw new Exception($"Failed to load singularity world map {component.SingularityWorldMap}");
        }

        SingularityWorldMapId = map.Value.Comp.MapId;
        Log.Info($"Created new singularity world at map ID {SingularityWorldMapId}");

        // Properly set up the teleporting effect
        var query = EntityQueryEnumerator<ESWarpDriveComponent>();
        while (query.MoveNext(out var driveEntity, out _))
        {
            EntityUid? teleportLocation = null;
            // Pick a random in-world marker to be the teleport location
            // (ideally itd pick any but whatever not rn)
            var locationQuery = EntityQueryEnumerator<ESSpawnRegionMarkerComponent>();
            while (locationQuery.MoveNext(out var regionEntity, out var marker))
            {
                if (marker.Area != SingularityWorldTeleportInWorld)
                    continue;

                teleportLocation = regionEntity;
            }

            if (teleportLocation == null)
            {
                throw new Exception("Singularity world map has no valid teleport locations for linking!");
            }

            // Set up link
            _linked.TryLink(driveEntity, teleportLocation.Value);
        }
    }
}
