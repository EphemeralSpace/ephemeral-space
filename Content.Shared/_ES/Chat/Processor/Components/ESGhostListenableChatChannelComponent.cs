using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESGhostListenableChatChannelSystem))]
public sealed partial class ESGhostListenableChatChannelComponent : Component;
