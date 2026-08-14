using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat.Radio.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a channel that relays a secondary message to a radio chat channel when spoken on.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRadioSystem))]
public sealed partial class ESWhisperRadioChatChannelComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ESChatChannelPrototype> RadioChannel;
}
