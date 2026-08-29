using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a chat channel that is broadcast to all players within a certain radius.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRangedChatChannelSystem))]
public sealed partial class ESRangedChatChannelComponent : Component
{
    /// <summary>
    /// Range that players can hear this chat channel from (relative to the source)
    /// </summary>
    [DataField]
    public float Range = 10;

    /// <summary>
    /// Whether the message can be received regardless of being in line of sight.
    /// </summary>
    [DataField]
    public bool RequireLOS = true;
}
