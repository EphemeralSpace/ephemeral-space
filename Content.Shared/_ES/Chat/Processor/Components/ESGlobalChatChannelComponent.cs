using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// Used for chat which is globally sent to all players.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESGlobalChatChannelSystem))]
public sealed partial class ESGlobalChatChannelComponent : Component;
