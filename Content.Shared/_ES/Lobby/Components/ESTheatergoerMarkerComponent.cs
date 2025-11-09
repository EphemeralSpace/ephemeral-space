using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Lobby.Components;

/// <summary>
/// an entity that counts as a theatergoer in the lobby
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESTheatergoerMarkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntProtoId<ActionComponent> ConfigurePrefsAction = "ESActionToggleConfigurePrefsWindow";

    [DataField, AutoNetworkedField]
    public EntityUid? ConfigurePrefsActionEntity;
}

public sealed partial class ESConfigurePrefsToggleActionEvent : InstantActionEvent;
