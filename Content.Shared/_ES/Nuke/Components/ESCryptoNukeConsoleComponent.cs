using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Nuke.Components;

/// <summary>
/// Console that tracks the nuke disk and can be hacked in order to reveal the nuke codes
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
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
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(0.25f);

    /// <summary>
    /// Whether this console has been hacked and compromised.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Compromised;

    /// <summary>
    ///     Controls whether the button to override warp drive security is disabled or not,
    ///     and whether the event will go through.
    ///     Set by warp drive whenever it gets fully charged.
    /// </summary>
    // this could maybe just use objective stuff in the same way traitor does
    // but thatd require atomizing the current warp drive obj into multiple and using prereqs also and idk.
    [DataField, AutoNetworkedField]
    public bool CanOverrideWarpDriveSecurity = false;

    /// <summary>
    /// Whether this terminal has been used to override warp drive security.
    /// </summary>
    /// <remarks>
    /// Can only be done when drive is fully charged + resets if not all terminals are overridden in some timeframe.
    /// </remarks>
    [DataField, AutoNetworkedField]
    public bool WarpDriveSecurityOverridden;
}

[Serializable, NetSerializable]
public sealed class ESCryptoNukeConsoleBuiState : BoundUserInterfaceState
{
    public List<NetCoordinates> DiskLocations = new();

    public List<string> Codes = new();
}

[Serializable, NetSerializable]
public sealed class ESSecurityOverrideCryptoNukeConsoleBuiMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public sealed class ESHackCryptoNukeConsoleBuiMessage : BoundUserInterfaceMessage;

[Serializable, NetSerializable]
public enum ESCryptoNukeConsoleUiKey : byte
{
    Key,
}

/// <summary>
///     Raised broadcast when a terminal overrides warp drive security.
/// </summary>
[ByRefEvent]
public record struct ESCryptoNukeSecurityOverridenEvent(EntityUid Terminal, EntityUid User);
