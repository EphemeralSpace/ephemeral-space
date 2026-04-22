using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESChatProcessorComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ESChatChannelPrototype> Channel;
}
