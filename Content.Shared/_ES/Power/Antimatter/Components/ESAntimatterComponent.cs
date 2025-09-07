using Content.Shared.Atmos;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Power.Antimatter.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedAntimatterSystem))]
public sealed partial class ESAntimatterComponent : Component
{
    /// <summary>
    /// The "health" of the antimatter.
    /// </summary>
    [DataField]
    public float Mass = 100;

    /// <summary>
    /// When <see cref="Mass"/> exceeds this value, it will overflow to other tiles
    /// </summary>
    [DataField]
    public float OverflowMass = 100;

    /// <summary>
    /// When <see cref="Mass"/> exceeds this value, it will explode
    /// </summary>
    [DataField]
    public float ExplosionMassAmount = 250;

    /// <summary>
    /// Sound made when the antimatter consumes an item.
    /// </summary>
    [DataField]
    public SoundSpecifier? ConsumeSound = new SoundCollectionSpecifier("RadiationPulse");

    /// <summary>
    /// Copy of itself used for spreading
    /// </summary>
    [DataField]
    public EntProtoId<ESAntimatterComponent> AntimatterProto = "ESAntimatter";

    /// <summary>
    /// Minimum size of antimatter being created.
    /// Used to prevent horrifically tiny antimatter pools.
    /// </summary>
    [DataField]
    public float MinSpreadMass = 5f;

    /// <summary>
    /// The gas that increases <see cref="Mass"/> for the antimatter.
    /// </summary>
    [DataField]
    public Gas GrowthGas = Gas.Plasma;

    /// <summary>
    /// Amount of <see cref="Mass"/> gained per mole of <see cref="GrowthGas"/>
    /// </summary>
    [DataField]
    public float GrowthPerGas = 47.5f;
}
