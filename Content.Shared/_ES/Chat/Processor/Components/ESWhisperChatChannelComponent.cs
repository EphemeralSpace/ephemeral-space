using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.Processor.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a channel which becomes partially obscured when outside an inner range.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESWhisperChatChannelSystem))]
public sealed partial class ESWhisperChatChannelComponent : Component
{
    /// <summary>
    /// Range at which the message is entirely unobfuscated.
    /// </summary>
    [DataField]
    public float ClearHearingRange = 2f;

    /// <summary>
    /// Chance of obfuscation per character per message when the message is received outside of <see cref="ClearHearingRange"/>
    /// </summary>
    [DataField]
    public float ObfuscateChance = 0.8f;
}
