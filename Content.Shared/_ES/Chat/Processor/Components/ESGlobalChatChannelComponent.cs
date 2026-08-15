using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> used for chat which is globally sent to all players.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESGlobalChatChannelSystem))]
public sealed partial class ESGlobalChatChannelComponent : Component;
