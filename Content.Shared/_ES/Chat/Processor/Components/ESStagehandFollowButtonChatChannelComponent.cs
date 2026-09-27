using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
///     Added to chat channels which should have entity name formatting prepended with a follow textlink when sending to stagehand clients.
///     When the button is clicked, the stagehand will teleport to the sender of the message.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESStagehandFollowButtonChatChannelComponent : Component;
