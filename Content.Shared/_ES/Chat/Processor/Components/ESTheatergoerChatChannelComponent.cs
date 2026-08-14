using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel broadcast to all lobby players (theatergoers)
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESTheaterChatChannelSystem))]
public sealed partial class ESTheatergoerChatChannelComponent : Component;
