using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESWhisperChatChannelSystem))]
public sealed partial class ESWhisperChatChannelComponent : Component
{
    [DataField]
    public float ClearHearingRange = 2f;

    [DataField]
    public float ObfuscateChance = 0.2f;
}
