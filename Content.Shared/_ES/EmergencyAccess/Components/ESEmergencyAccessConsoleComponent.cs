using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.EmergencyAccess.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true)]
[Access(typeof(ESSharedEmergencyAccessSystem))]
public sealed partial class ESEmergencyAccessConsoleComponent : Component
{
    [DataField, AutoNetworkedField]
    public string CurrentKey = string.Empty;

    [DataField, AutoNetworkedField]
    public bool HasValidState;

    [DataField, AutoNetworkedField]
    public bool EmergencyEnabled;

    [DataField, AutoNetworkedField]
    public bool BoltEnabled;

    [DataField, AutoNetworkedField]
    public bool PowerEnabled;

    [DataField]
    public int DegradationDoorSabotageCount = 10;

    [DataField]
    public HashSet<ProtoId<AccessLevelPrototype>> IgnoredAccessList = ["Maintenance"];
}

[Serializable, NetSerializable]
public enum ESEmergencyAccessConsoleUiKey : byte
{
    Key,
}

[Serializable, NetSerializable]
public sealed class ESEmergencyAccessSearchBuiMessage(string key) : BoundUserInterfaceMessage
{
    public string Key = key;
}

[Serializable, NetSerializable]
public sealed class ESEmergencyAccessToggleBuiMessage : BoundUserInterfaceMessage;
