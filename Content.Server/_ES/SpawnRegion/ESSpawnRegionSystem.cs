using Content.Server.Atmos.EntitySystems;
using Content.Shared._ES.SpawnRegion;
using Content.Shared.Atmos;

namespace Content.Server._ES.SpawnRegion;

public sealed partial class ESSpawnRegionSystem : ESSharedSpawnRegionSystem
{
    [Dependency] private AtmosphereSystem _atmosphere = default!;

    protected override bool IsMarkerPressureSafe(EntityUid grid, EntityUid? map, Vector2i indices)
    {
        if (_atmosphere.GetTileMixture(grid, map, indices) is not { } tileMixture)
            return false;

        switch (tileMixture.Pressure)
        {
            case <= Atmospherics.WarningLowPressure:
            case >= Atmospherics.WarningHighPressure:
                return false;
        }

        return true;
    }

    protected override bool IsMarkerTemperatureSafe(EntityUid grid, EntityUid? map, Vector2i indices)
    {
        if (_atmosphere.GetTileMixture(grid, map, indices) is not { } tileMixture)
            return false;

        switch (tileMixture.Temperature) // Arbitrary constants taken from AtmosphereSystem.IsMixtureProbablySafe
        {
            case <= 260:
            case >= 360:
                return false;
        }

        return true;
    }
}
