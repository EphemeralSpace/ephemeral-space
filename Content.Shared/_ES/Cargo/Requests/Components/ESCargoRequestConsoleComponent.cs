using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Cargo.Requests.Components;

/// <summary>
/// A client that interfaces with <see cref="ESCargoRequestStationComponent"/> to create requests and change their status
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedCargoRequestSystem))]
public sealed partial class ESCargoRequestConsoleComponent : Component
{
    public const int MaxBodyLength = 512;

    public const int MaxIdLength = 24;

    /// <summary>
    /// Whether the update indicator is currently enabled
    /// </summary>
    [DataField]
    public bool UpdateIndicator;

    /// <summary>
    /// If this is the master console, then it can make status updates and view all orders
    /// </summary>
    [DataField]
    public bool MasterConsole;

    /// <summary>
    /// Unique ID used for identifying where orders are from and who has visibility
    /// </summary>
    [DataField, AutoNetworkedField]
    public string ConsoleId = string.Empty;

    /// <summary>
    /// Base value of <see cref="ConsoleId"/>
    /// </summary>
    [DataField]
    public LocId DefaultConsoleId = "es-cargo-request-console-dept-default";
}

[Serializable, NetSerializable]
public enum ESCargoRequestConsoleVisuals : byte
{
    Update,
}

[Serializable, NetSerializable]
public enum ESCargoRequestConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ESSetDepartmentIdMessage(string body) : BoundUserInterfaceMessage
{
    public string DepartmentId = body;
}

[Serializable, NetSerializable]
public sealed class ESCreateCargoRequestMessage(string body) : BoundUserInterfaceMessage
{
    public string Body = body;
}

[Serializable, NetSerializable]
public sealed class ESSetCargoRequestStatusMessage(int requestId, ESCargoRequestStatus newStatus) : BoundUserInterfaceMessage
{
    public int RequestId = requestId;
    public ESCargoRequestStatus NewStatus = newStatus;
}
