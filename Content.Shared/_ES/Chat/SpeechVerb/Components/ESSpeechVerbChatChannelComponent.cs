using Robust.Shared.GameStates;

namespace Content.Shared._ES.Chat.SpeechVerb.Components;

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
