using Content.Server.Atmos.EntitySystems;
using Content.Shared._ES.SpawnRegion;
using Content.Shared._ES.SpawnRegion.Components;
using Content.Shared.Atmos;

namespace Content.Server._ES.SpawnRegion;

public sealed class ESSpawnRegionSystem : ESSharedSpawnRegionSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override bool IsMarkerPressureSafe(Entity<ESSpawnRegionMarkerComponent, TransformComponent> ent)
    {
        if (_atmosphere.GetTileMixture((ent, ent)) is not { } tileMixture)
            return false;

        switch (tileMixture.Pressure)
        {
            case <= Atmospherics.WarningLowPressure:
            case >= Atmospherics.WarningHighPressure:
                return false;
        }

        return true;
    }

    protected override bool IsMarkerTemperatureSafe(Entity<ESSpawnRegionMarkerComponent, TransformComponent> ent)
    {
        if (_atmosphere.GetTileMixture((ent, ent)) is not { } tileMixture)
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
