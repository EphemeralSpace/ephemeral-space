using Content.Shared._ES.Chat.Components;

namespace Content.Server._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> that overrides an entity's name in a chat message with the owning player's
/// username. If the owning player is an admin, this will also provide them with their specialty color.
/// </summary>
[RegisterComponent]
[Access(typeof(ESUsernameChatChannelSystem))]
public sealed partial class ESUsernameChatChannelComponent : Component;
