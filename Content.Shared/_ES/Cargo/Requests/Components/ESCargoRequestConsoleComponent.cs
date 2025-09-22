using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared._ES.Cargo.Requests.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedCargoRequestSystem))]
public sealed partial class ESCargoRequestConsoleComponent : Component
{
    public const int MaxBodyLength = 512;

    [DataField(customTypeSerializer: typeof(FlagSerializer<ESCargoRequestStatus>))]
    public ESCargoRequestStatus SettableStatuses = ESCargoRequestStatus.Pending | ESCargoRequestStatus.Cancelled;

    [DataField]
    public bool UpdateIndicator;

    [DataField]
    public bool MasterConsole;

    [DataField, AutoNetworkedField]
    public string DepartmentString = string.Empty;
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
