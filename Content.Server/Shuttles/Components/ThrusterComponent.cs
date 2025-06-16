using System.Numerics;
using Content.Server.Shuttles.Systems;
using Content.Shared.Atmos;
using Content.Shared.Damage;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server.Shuttles.Components
{
    [RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
    [Access(typeof(ThrusterSystem))]
    public sealed partial class ThrusterComponent : Component
    {
        /// <summary>
        /// Whether the thruster has been force to be enabled / disabled (e.g. VV, interaction, etc.)
        /// </summary>
        [DataField, ViewVariables(VVAccess.ReadWrite)]
        public bool Enabled { get; set; } = true;

        /// <summary>
        /// This determines whether the thruster is actually enabled for the purposes of thrust
        /// </summary>
        public bool IsOn;

        // Need to serialize this because RefreshParts isn't called on Init and this will break post-mapinit maps!
        [ViewVariables(VVAccess.ReadWrite), DataField("thrust")]
        public float Thrust = 100f;

        [DataField("thrusterType")]
        public ThrusterType Type = ThrusterType.Linear;

        [DataField("burnShape")] public List<Vector2> BurnPoly = new()
        {
            new Vector2(-0.4f, 0.5f),
            new Vector2(-0.1f, 1.2f),
            new Vector2(0.1f, 1.2f),
            new Vector2(0.4f, 0.5f)
        };

        /// <summary>
        /// How much damage is done per second to anything colliding with our thrust.
        /// </summary>
        [DataField("damage")] public DamageSpecifier? Damage = new();

        [DataField("requireSpace")]
        public bool RequireSpace = true;

        // Used for burns

        public List<EntityUid> Colliding = new();

        public bool Firing = false;

        /// <summary>
        /// How often thruster deals damage.
        /// </summary>
        [DataField]
        public TimeSpan FireCooldown = TimeSpan.FromSeconds(2);

        /// <summary>
        /// Next time we tick damage for anyone colliding.
        /// </summary>
        [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
        public TimeSpan NextFire = TimeSpan.Zero;

        #region _ES

        /// <summary>
        /// Designates whether the thruster is a fueled thruster.
        /// Fueled thrusters are those that require some type of gas medium to operate.
        /// They are automatically enabled/disabled if a fuel gas is present in the inlet.
        /// Normal interactions that would enable/disable the thruster are blocked.
        /// </summary>
        [DataField]
        public bool IsFueledThruster;

        /// <summary>
        /// Designates whether the thruster is allowed to be active.
        /// This is set by the thruster's atmosphere update loop, based on if the thruster is fueled or not.
        /// Ignored if the thruster is not a fueled thruster.
        /// </summary>
        [DataField]
        public bool IsAllowedActive;

        /// <summary>
        /// Designates whether the thruster requires fuel to operate.
        /// </summary>
        [DataField]
        public bool RequiresFuel;

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
        /// The base thrust of the thruster.
        /// </summary>
        [DataField]
        public float BaseThrust = 100f;

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
        public float MaxGasConsumptionEfficiency = 0.1f;

        /// <summary>
        /// The minimum thrust multiplier the thruster can reach.
        /// </summary>
        [DataField]
        public float MinGasThrustMultiplier = 0.1f;

        /// <summary>
        /// The minimum efficiency the thruster can reach.
        /// </summary>
        [DataField]
        public float MinGasConsumptionEfficiency = 5f;

        /// <summary>
        /// The minimum change from the previous thrust value needed to apply the new thrust value to the thruster.
        /// Intended to prevent constantly applying values that are too small to matter.
        /// </summary>
        [DataField]
        public float PreviousValueComparisonTolerance = 1f;

        #endregion

        /// <summary>
        /// The maximum rate at which the thruster can update its buffs/nerfs based on gas input.
        /// </summary>
        /// <remarks>Recomputing and resetting the thrust each atmos tick seems expensive,
        /// so just putting this here for now.</remarks>
        [DataField]
        public TimeSpan GasBenefitsUpdateInterval = TimeSpan.FromSeconds(5);

        /// <summary>
        /// Holds a pair of GasMixture and information on its associated buffs/nerfs.
        /// </summary>
        [DataField]
        public ThrusterGasMixturePair[] GasMixturePair = [new()];

        #endregion

    }

    #region _ES

    /// <summary>
    /// Extremely cursed object that pairs a GasMixture with information
    /// for <see cref="ThrusterComponent"/> to use.
    /// </summary>
    /// <remarks>We do this to keep up with whatever happens to <see cref="GasMixture"/>
    /// upstream, and it's easy to define gas ratios in the array built into it, rather
    /// than effectively duplicating the code.</remarks>
    [Serializable]
    [DataDefinition]
    public sealed partial class ThrusterGasMixturePair
    {
        /// <summary>
        /// The gas mixture that this pair is based on.
        /// </summary>
        [DataField]
        public GasMixture Mixture = new();

        /// <summary>
        /// The thrust multiplier that this gas mixture provides.
        /// This can go in both directions.
        /// </summary>
        /// <example>If set to 1.5, the thrust will be increased by 50%.</example>
        [DataField]
        public float ThrustMultiplier = 1f;

        /// <summary>
        /// The gas consumption efficiency that this gas mixture provides.
        /// This can go in both directions.
        /// </summary>
        /// <example>If set to 0.5, the thruster will see 50% fuel consumption.</example>
        [DataField]
        public float ConsumptionEfficiency = 1f;

        /// <summary>
        /// Whether this gas mixture is considered fuel.
        /// The presence of this gas mixture will allow the thruster to function.
        /// If no fuel gasses are present, the thruster will simply be disabled.
        /// If this is set to false, the gas mixture
        /// </summary>
        [DataField]
        public bool IsFuel = true;

        /// <summary>
        /// Represents the condition for the benefits applied from the gas mixture associated with the thruster.
        /// </summary>
        [DataField]
        public GasMixtureBenefitsCondition BenefitsCondition = GasMixtureBenefitsCondition.None;
    }

        /// <summary>
        /// Defines the condition under which a <see cref="ThrusterGasMixturePair"/> gas mixture's benefits apply.
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

    #endregion

    public enum ThrusterType
    {
        Linear,
        // Angular meaning rotational.
        Angular,
    }
}
