using Content.Server._ES.Filth;
using Content.Server.Atmos;
using Content.Server.Atmos.EntitySystems;
using Content.Shared.Atmos;
using Content.Shared.Atmos.Reactions;
using JetBrains.Annotations;

namespace Content.Server._ES.Atmos.Reactions;

[UsedImplicitly]
public sealed partial class ESMiasmaDirtReaction : IGasReactionEffect
{
    [DataField]
    public float DecalCountDivisor = 1.5f;

    public ReactionResult React(GasMixture mixture, IGasMixtureHolder? holder, AtmosphereSystem atmosphereSystem, float heatScale)
    {
        if (holder is not TileAtmosphere location)
            return ReactionResult.NoReaction;

        var mols = mixture.GetMoles(Gas.Miasma);
        var decalCount = Math.Min((int)MathF.Floor(mols / DecalCountDivisor), ESMiasmaSystem.MaxDirtDecalsPerTile);
        if (decalCount == 0)
            return ReactionResult.NoReaction;

        atmosphereSystem.Miasma.TryAddDirtDecalsToTile(location.GridIndex, location.GridIndices, decalCount);
        return ReactionResult.Reacting;
    }
}
