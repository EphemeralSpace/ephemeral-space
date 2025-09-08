using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Power.Antimatter.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ESSharedAntimatterSystem))]
public sealed partial class ESAntimatterConverterComponent : Component
{
    /// <summary>
    /// when the next update will occur.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// The amount of mass removed from each piece of antimatter each update.
    /// </summary>
    [DataField]
    public float RemovalAmount = 2f;

    /// <summary>
    /// The amount of energy produced per mass removed from antimatter.
    /// </summary>
    [DataField]
    public float EnergyPerMass = 7500;

    /// <summary>
    /// If the converter is broken, then nothing will occur.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Broken;
}

[Serializable, NetSerializable]
public enum ESAntimatterConverterVisuals : byte
{
    Draining,
    Broken,
}
