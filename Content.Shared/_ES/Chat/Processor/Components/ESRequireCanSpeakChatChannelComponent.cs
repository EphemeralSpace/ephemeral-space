using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRequireCanSpeakChatChannelSystem))]
public sealed partial class ESRequireCanSpeakChatChannelComponent : Component;
