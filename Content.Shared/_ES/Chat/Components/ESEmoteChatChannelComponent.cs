using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESEmoteSystem))]
public sealed partial class ESEmoteChatChannelComponent : Component
{
    [DataField]
    public bool EmoteFromChat = true;
}
