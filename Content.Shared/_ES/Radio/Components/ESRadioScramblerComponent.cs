using Robust.Shared.GameStates;

namespace Content.Shared._ES.Radio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESRadioScramblerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hacked;
}
