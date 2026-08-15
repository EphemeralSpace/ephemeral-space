using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel that requires the source to be able to speak audibly.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESActionBlockerChatChannelSystem))]
public sealed partial class ESRequireCanSpeakChatChannelComponent : Component;
