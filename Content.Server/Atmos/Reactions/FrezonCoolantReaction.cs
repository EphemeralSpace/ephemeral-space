using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class FrezonCoolantReaction : IGasReactionEffect
{
    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        var oldHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
        var temperature = mixture.Temperature;

        var energyModifier = 1f;
        var scale = (temperature - Atmospherics.FrezonCoolLowerTemperature) /
                    (Atmospherics.FrezonCoolMidTemperature - Atmospherics.FrezonCoolLowerTemperature);

        if (scale > 1f)
        {
            // Scale energy but not frezon usage if we're in a very, very hot place
            energyModifier = Math.Min(scale, Atmospherics.FrezonCoolMaximumEnergyModifier);
            scale = 1f;
        }

        if (scale <= 0)
            return ReactionResult.NoReaction;

        var initialFrezon = mixture.GetMoles(Gas.Cryogas);

        var burnRate = initialFrezon * scale / Atmospherics.FrezonCoolRateModifier;

        var energyReleased = 0f;
        if (burnRate > Atmospherics.MinimumHeatCapacity)
        {
            energyReleased = burnRate * Atmospherics.FrezonCoolEnergyReleased * energyModifier;
        }

        energyReleased /= heatScale; // adjust energy to make sure speedup doesn't cause mega temperature rise
        if (energyReleased >= 0f)
            return ReactionResult.NoReaction;

        var newHeatCapacity = atmosphereSystem.GetHeatCapacity(mixture, true);
        if (newHeatCapacity > Atmospherics.MinimumHeatCapacity)
            mixture.Temperature = (temperature * oldHeatCapacity + energyReleased) / newHeatCapacity;

        return ReactionResult.Reacting;
    }
}
