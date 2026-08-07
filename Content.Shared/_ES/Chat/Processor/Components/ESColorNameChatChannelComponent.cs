using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESColorNameChatChannelSystem))]
public sealed partial class ESColorNameChatChannelComponent : Component;
