using Robust.Shared.GameStates;

namespace Content.Shared._ES.EmergencyAccess.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedEmergencyAccessSystem))]
public sealed partial class ESEmergencyAccessDoorComponent : Component
{
    [DataField, AutoNetworkedField]
    public string Key = string.Empty;
}
