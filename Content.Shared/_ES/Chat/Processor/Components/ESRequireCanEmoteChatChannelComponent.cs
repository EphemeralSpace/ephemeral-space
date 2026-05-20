using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESActionBlockerChatChannelSystem))]
public sealed partial class ESRequireCanEmoteChatChannelComponent : Component;
