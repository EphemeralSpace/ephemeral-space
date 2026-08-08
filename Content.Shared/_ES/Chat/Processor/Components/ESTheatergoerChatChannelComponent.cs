using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESTheaterChatChannelSystem))]
public sealed partial class ESTheatergoerChatChannelComponent : Component;
