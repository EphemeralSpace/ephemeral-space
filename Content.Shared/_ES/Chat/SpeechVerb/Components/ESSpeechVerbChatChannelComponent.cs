using Content.Shared._ES.Chat.Components;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.SpeechVerb.Components;

/// <summary>
/// <see cref="ESChatProcessorComponent"/> for a channel that formats the overall message based on a verb derived from the content of the message and the source.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSpeechVerbSystem))]
public sealed partial class ESSpeechVerbChatChannelComponent : Component
{
    /// <summary>
    /// Format string to use for normal text
    /// </summary>
    [DataField(required: true)]
    public LocId Format;

    /// <summary>
    /// Format string to use for bolded text
    /// </summary>
    [DataField(required: true)]
    public LocId BoldFormat;
}
