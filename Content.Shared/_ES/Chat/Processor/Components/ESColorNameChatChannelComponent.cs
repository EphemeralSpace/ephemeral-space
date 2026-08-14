using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> that applies an additional unique coloring to the chat message's name field
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESColorNameChatChannelSystem))]
public sealed partial class ESColorNameChatChannelComponent : Component;
