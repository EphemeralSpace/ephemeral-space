using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Components;

/// <summary>
/// Legacy behavior for <see cref="ESChatProcessorComponent"/> that covers transformations for various "spoken" behavior.
/// This is not really a logical distinction, but it can be though to handle any chat channel in which a person physically "speaks."
/// So things like accents, voice obfuscation, and events relating to "speaking" are handled here. For Now.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSpeechSystem))]
public sealed partial class ESSpeechChatChannelComponent : Component;
