using Content.Server.Atmos.EntitySystems;
using Content.Shared._ES.Area;
using Content.Shared._ES.Area.Components;
using Content.Shared.Atmos;

namespace Content.Server._ES.Area;

public sealed class ESAreaSystem : ESSharedAreaSystem
{
    [Dependency] private readonly AtmosphereSystem _atmosphere = default!;

    protected override bool IsMarkerPressureSafe(Entity<ESAreaMarkerComponent, TransformComponent> ent)
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

    protected override bool IsMarkerTemperatureSafe(Entity<ESAreaMarkerComponent, TransformComponent> ent)
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
