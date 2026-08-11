using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Radio.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRadioSystem))]
public sealed partial class ESRadioChatChannelComponent : Component
{
    [DataField]
    public bool RequireServer = true;
}
