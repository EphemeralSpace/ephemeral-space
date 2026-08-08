using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESChatProcessorComponent : Component
{
    [DataField(required: true), AutoNetworkedField]
    public ProtoId<ESChatChannelPrototype> Channel;
}
