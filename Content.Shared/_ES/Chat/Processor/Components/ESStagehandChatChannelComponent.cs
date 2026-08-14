using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> that is broadcast to all stagehands.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESTheaterChatChannelSystem))]
public sealed partial class ESStagehandChatChannelComponent : Component;
