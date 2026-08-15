using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Obfuscation;

/// <summary>
/// This is used for a chat channel which uses visual identity to override the chat message name.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESIdentityChatChannelSystem))]
public sealed partial class ESIdentityChatChannelComponent : Component;
