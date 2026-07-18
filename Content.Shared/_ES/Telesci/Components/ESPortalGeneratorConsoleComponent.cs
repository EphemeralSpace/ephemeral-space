using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Telesci.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentPause]
public sealed partial class ESPortalGeneratorConsoleComponent : Component
{
    /// <summary>
    /// Time between updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateDelay = TimeSpan.FromSeconds(1);

    /// <summary>
    /// Time when next update occurs
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdateTime;
}

[Serializable, NetSerializable]
public enum ESPortalGeneratorConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ESPortalGeneratorConsoleBuiState : BoundUserInterfaceState
{
    public float Charge;
    public bool Interrupted;
    public bool FinalPhase;
}

[Serializable, NetSerializable]
public sealed class ESActivePortalGeneratorBuiMessage : BoundUserInterfaceMessage;
