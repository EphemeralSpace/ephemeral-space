using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Telesci.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ESSharedTelesciSystem))]
public sealed partial class ESPortalGeneratorComponent : Component
{
    /// <summary>
    /// Time between updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time when next update occurs
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// Amount of time accumulated when <see cref="Powered"/> is true
    /// </summary>
    [DataField, AutoNetworkedField]
    public TimeSpan AccumulatedChargeTime = TimeSpan.Zero;

    /// <summary>
    /// How long <see cref="AccumulatedChargeTime"/> must be for <see cref="Charged"/> to be true
    /// </summary>
    [DataField]
    public TimeSpan ChargeDuration = TimeSpan.FromMinutes(6f);

    /// <summary>
    /// Whether the generator is charged
    /// </summary>
    [ViewVariables]
    public bool Charged => AccumulatedChargeTime > ChargeDuration;

    /// <summary>
    /// Whether the generator is powered.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Powered;

    /// <summary>
    /// How many portal event threats are left from the last wave.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int ThreatsLeft = 0;
}

[Serializable, NetSerializable]
public enum ESPortalGeneratorVisuals : byte
{
    Charged,
}
