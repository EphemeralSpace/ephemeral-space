using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Nuke.Components;

/// <summary>
/// Console that tracks the nuke disk and can be hacked in order to reveal the nuke codes
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ESSharedCryptoNukeSystem))]
public sealed partial class ESCryptoNukeConsoleComponent : Component
{
    /// <summary>
    /// Time at which the console UI will update the positions of the disks
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    /// Delay between sending UI state updates
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(2.5f);

    /// <summary>
    /// Whether this console has been hacked and compromised.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Compromised;
}

[Serializable, NetSerializable]
public sealed class ESCryptoNukeConsoleBuiState : BoundUserInterfaceState
{
    public List<NetCoordinates> DiskLocations = new();

    public List<string> Codes = new();

    // This kinda sucks having to network this to all clients
    // but there's not much else I can do really.
    // TODO: make this unnecessary once we have better objectives
    public bool CanHack;
}

[Serializable, NetSerializable]
public sealed class ESHackCryptoNukeConsoleBuiMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum ESCryptoNukeConsoleUiKey : byte
{
    Key,
}
