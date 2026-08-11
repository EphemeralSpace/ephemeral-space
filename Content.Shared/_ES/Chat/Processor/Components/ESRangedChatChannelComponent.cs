using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESRangedChatChannelSystem))]
public sealed partial class ESRangedChatChannelComponent : Component
{
    /// <summary>
    /// Range that players can hear this chat channel from (relative to the source)
    /// </summary>
    [DataField]
    public float Range = 10;

    [DataField]
    public bool RequireLOS = true;
}
