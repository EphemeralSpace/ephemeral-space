// ES START
// This completely overwrites the upstream thruster code. that's not great.
// Either port and merge these or break this out.
// ES END
using System.Numerics;
using Content.Server.Shuttles.Systems;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components;

/// <summary>
/// Entities with this component act as a thruster, contributing potential impulse or a
/// "movement opportunity" to the ShuttleComponent they are attached to.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
[Access(typeof(ThrusterSystem))]
public sealed partial class ThrusterComponent : Component
{
    /// <summary>
    /// Whether the thruster is currently enabled or not.
    /// This is a toggle intended for in-game user interactions. See <see cref="IsOn"/>
    /// for limitation based toggling.
    /// </summary>
    [DataField]
    public bool Enabled = true;

    /// <summary>
    /// A toggle for determining whether the thruster is allowed to fire.
    /// Mechanical conditions are checked for this (whether it be powered, has fuel, etc.)
    /// </summary>
    [DataField]
    public bool IsOn;

    /// <summary>
    /// Is this thruster currently firing or providing impulse?
    /// </summary>
    [DataField]
    public bool Firing;

    /// <summary>
    /// The current amount of thrust this thruster is contributing.
    /// </summary>
    [DataField]
    public float Thrust = 100f;

    /// <summary>
    /// The baseline thrust of this thruster. Any thruster buffs/nerfs use this value
    /// as a baseline to calculate the final thrust value.
    /// </summary>
    [DataField]
    public float BaseThrust = 100f;

    /// <summary>
    /// The type of thrust this thruster contributes to the shuttle.
    /// </summary>
    [DataField]
    public ThrusterType ThrusterType = ThrusterType.Linear;

    /// <summary>
    /// Whether this thruster requires exposure to space.
    /// The tile south of the thruster is checked to see if it is scaffolding/lattice
    /// or empty space.
    /// </summary>
    [DataField]
    public bool RequireSpace = true;

    /// <summary>
    /// Whether this thruster requires power to operate.
    /// </summary>
    /// <remarks>Useful for thrusters that behave like simple converging-diverging
    /// propellant thrusters, rather than ion thrusters.</remarks>
    [DataField]
    public bool RequirePower = true;

    #region Damage

    /// <summary>
    /// Whether this thruster deals damage to entities caught in its thrust cone,
    /// defined in <see cref="BurnPoly"/>.
    /// </summary>
    public bool DealDamage = true;

    /// <summary>
    /// A poly that stores the shape of a thrust cone collision box.
    /// Used to determine if someone is colliding with a thruster,
    /// so we can start dealing burn damage to them.
    /// </summary>
    [DataField]
    public List<Vector2> BurnPoly =
    [
        new(-0.4f, 0.5f),
        new(-0.1f, 1.2f),
        new(0.1f, 1.2f),
        new(0.4f, 0.5f),
    ];

    /// <summary>
    /// A list containing all entities currently colliding with our <see cref="BurnPoly"/>.
    /// Entities in this list will take damage via <see cref="Damage"/> until they leave the
    /// list.
    /// </summary>
    [DataField]
    public List<EntityUid> Colliding = new();

    /// <summary>
    /// The damage that is to be done to the person colliding with our
    /// <see cref="BurnPoly"/>.
    /// </summary>
    [DataField]
    public DamageSpecifier? Damage = new();

    /// <summary>
    /// How often the thruster deals damage to entities in <see cref="Colliding"/>.
    /// </summary>
    [DataField]
    public TimeSpan DamageCooldown = TimeSpan.FromSeconds(2);

    /// <summary>
    /// The next time at which we deal damage for anyone in the <see cref="Colliding"/> list.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextFire = TimeSpan.Zero;

    #endregion

    #region Fueled Thrusters

    /// <summary>
    /// Designates whether the thruster is a fueled thruster.
    /// Fueled thrusters are those that receive benefits or drawbacks from a processed gas.
    /// They are automatically enabled/disabled via <see cref="IsOn"/> a fuel gas is present in the inlet.
    /// </summary>
    [DataField]
    public bool IsFueledThruster;

    /// <summary>
    /// Designates whether the thruster requires a fuel-designated gas to operate.
    /// </summary>
    /// <remarks>A thruster that doesn't require fuel can still receive benefits or drawbacks,
    /// but it doesn't need fuel to function.</remarks>
    [DataField]
    public bool RequiresFuel;

    /// <summary>
    /// Whether this thruster is currently fueled with a valid fuel gas.
    /// </summary>
    [DataField]
    public bool HasFuel;

    /// <summary>
    /// Name of the inlet port for the thruster.
    /// </summary>
    [DataField]
    public string InletName = "inlet";

    /// <summary>
    /// The thrust value from last atmos tick.
    /// </summary>
    [DataField]
    public float PreviousThrust;

    /// <summary>
    /// The baseline gas consumption rate of the thruster, in moles per atmos update tick.
    /// </summary>
    [DataField]
    public float BaseGasConsumptionRate = 10f;

    /// <summary>
    /// The thruster's current thrust multiplier.
    /// </summary>
    [DataField]
    public float GasThrustMultiplier = 1f;

    /// <summary>
    /// The current efficiency of the thruster's gas consumption.
    /// Higher values mean less gas is consumed for the same thrust, and vice versa.
    /// </summary>
    [DataField]
    public float GasConsumptionEfficiency = 1f;

    #region Min/Max Clamps

    /// <summary>
    /// The maximum thrust multiplier the thruster can reach.
    /// </summary>
    [DataField]
    public float MaxGasThrustMultiplier = 5f;

    /// <summary>
    /// The maximum efficiency the thruster can reach.
    /// </summary>
    [DataField]
    public float MaxGasConsumptionEfficiency = 5;

    /// <summary>
    /// The minimum thrust multiplier the thruster can reach.
    /// </summary>
    [DataField]
    public float MinGasThrustMultiplier = 0.1f;

    /// <summary>
    /// The minimum efficiency the thruster can reach.
    /// </summary>
    [DataField]
    public float MinGasConsumptionEfficiency = 0.1f;

    /// <summary>
    /// The minimum change from the previous thrust value needed to apply the new thrust value to the thruster.
    /// Intended to prevent constantly applying values that are too small to matter.
    /// </summary>
    [DataField]
    public float PreviousValueComparisonTolerance = 1f;

    #endregion

    /// <summary>
    /// Holds a pair of <see cref="GasMixture"/> and information on its associated buffs/nerfs.
    /// </summary>
    [DataField]
    public ThrusterGasMixture[] GasMixturePair = [new()];

    #endregion

}

    /// <summary>
    /// Object that pairs a <see cref="GasMixture"/> with buff/nerf information.
    /// We use this <see cref="GasMixture"/> to store ratio information, which is compared via various
    /// methods to determine when the buffs/nerfs associated with the <see cref="GasMixture"/> apply.
    /// </summary>
    /// <remarks>We do this to keep up with whatever happens to <see cref="GasMixture"/>
    /// upstream, and it's easy to define gas ratios in the array built into it, rather
    /// than effectively duplicating the code.</remarks>
    [Serializable]
    [DataDefinition]
    public sealed partial class ThrusterGasMixture
    {
        /// <summary>
        /// The <see cref="GasMixture"/> that stores a ratio of gas.
        /// </summary>
        /// <example>A target ratio of 50% oxygen,
        /// 50% nitrogen would have 0.5 oxygen and 0.5 nitrogen stored in it.</example>
        [DataField]
        public GasMixture Mixture = new();

        /// <summary>
        /// The thrust multiplier that this <see cref="GasMixture"/> provides.
        /// This can go in both directions.
        /// </summary>
        /// <example>If set to 1.5, the thrust will be increased by 50%.</example>
        [DataField]
        public float ThrustMultiplier = 1f;

        /// <summary>
        /// The gas consumption efficiency that this <see cref="GasMixture"/> provides.
        /// This can go in both directions.
        /// </summary>
        /// <example>If set to 0.5, the thruster will see 50% fuel consumption.</example>
        [DataField]
        public float ConsumptionEfficiency = 1f;

        /// <summary>
        /// Whether this <see cref="GasMixture"/> is considered fuel.
        /// The presence of this <see cref="GasMixture"/> will allow the thruster to function,
        /// presuming the thruster requires fuel.
        /// If no fuel gases are present, the thruster will simply be disabled.
        /// </summary>
        [DataField]
        public bool IsFuel = true;

        /// <summary>
        /// Represents the condition for the benefits applied from the <see cref="GasMixture"/> associated with the thruster.
        /// </summary>
        [DataField]
        public GasMixtureBenefitsCondition BenefitsCondition = GasMixtureBenefitsCondition.None;
    }

        /// <summary>
        /// Defines the condition under which a <see cref="ThrusterGasMixture"/> <see cref="GasMixture"/>'s benefits apply.
        /// </summary>
        [Serializable]
        public enum GasMixtureBenefitsCondition : byte
        {
            /// <summary>
            /// The benefits apply as long as the required gas is present in the mixture.
            /// </summary>
            None,

            /// <summary>
            /// The gas must be at a certain percentage of the mixture for the benefits to apply.
            /// </summary>
            /// <example>
            /// <para>If the target <see cref="GasMixture"/> contains 50% nitrogen, and the inlet gas contains
            /// 65% nitrogen, rest partial gasses, all the benefits will be applied.</para>
            /// </example>
            /// <para>If the target <see cref="GasMixture"/> is 50% oxygen and 50% nitrogen,
            /// and the inlet gas contains 49% oxygen and 51% nitrogen,
            /// no benefits will be applied.</para>
            SingleThreshold,

            /// <summary>
            /// Similar to <see cref="SingleThreshold"/>, but the gas must be pure, and its effectiveness
            /// is reduced based on how pure it is.
            /// </summary>
            /// <example>
            /// <para>If the target <see cref="GasMixture"/> is 50% oxygen and 50% nitrogen, and the inlet gas is
            /// 25% oxygen, 25% nitrogen, only 50% of the benefits will be applied.</para>
            /// <para>If the target <see cref="GasMixture"/> is 50% oxygen, and the inlet gas is 25% oxygen,
            /// rest partial gasses, only 50% of the benefits will be applied.</para>
            /// </example>
            SingleThresholdPure,

            /// <summary>
            /// The similarity of the <see cref="GasMixture"/>s is determined, and the effects are applied
            /// depending on how similar the two mixtures are.
            /// </summary>
            /// <example>If the mixture is 50% pure, it will only give 50% of its benefits.</example>
            Pure,
        }


/// <summary>
/// An enum for determining the type of impulse this thruster contributes to a shuttle grid.
/// </summary>
public enum ThrusterType
{
    /// <summary>
    /// The thruster will provide linear impulse (think X, Y).
    /// </summary>
    Linear,
    /// <summary>
    /// The thruster will provide angular impulse (think rotation).
    /// </summary>
    Angular,
}
