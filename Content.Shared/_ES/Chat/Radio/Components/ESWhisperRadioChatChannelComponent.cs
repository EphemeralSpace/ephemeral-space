using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Radio.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRadioSystem))]
public sealed partial class ESWhisperRadioChatChannelComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ESChatChannelPrototype> RadioChannel;
}
