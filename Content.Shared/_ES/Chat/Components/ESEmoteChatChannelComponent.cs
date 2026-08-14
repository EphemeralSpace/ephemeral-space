using Content.Shared.Chat.Prototypes;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel that translates sent text into <see cref="EmotePrototype"/>
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESEmoteSystem))]
public sealed partial class ESEmoteChatChannelComponent : Component
{
    [DataField]
    public bool EmoteFromChat = true;
}
