using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Cargo.Requests.Components;

/// <summary>
/// Manages central data for all cargo requests.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedCargoRequestSystem))]
public sealed partial class ESCargoRequestStationComponent : Component
{
    /// <summary>
    /// The next ID to be used for <see cref="Requests"/>
    /// </summary>
    [DataField, AutoNetworkedField]
    public int NextRequestId;

    /// <summary>
    /// A canonical list of all requests on the station.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<int, ESCargoRequest> Requests = new();
}

[Serializable, NetSerializable, Flags]
public enum ESCargoRequestStatus : byte
{
    Pending,
    InProgress,
    Completed,
    Denied,
    Cancelled,
}

[Serializable, NetSerializable]
[DataDefinition]
public sealed partial class ESCargoRequest
{
    [DataField] public string User = string.Empty;
    [DataField] public string Department = string.Empty;
    [DataField] public string RequestBody = string.Empty;

    [DataField] public ESCargoRequestStatus Status;
}
