using Content.Shared.Actions;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.SecretIdentity.Stalker.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESStalkerTargetingComponent : Component
{
    [DataField]
    public List<EntityUid> Targets = new();

    [DataField, AutoNetworkedField]
    public List<string> TargetNames = new();
}

public sealed partial class ESSelectStalkerTargetActionEvent : EntityTargetActionEvent;
