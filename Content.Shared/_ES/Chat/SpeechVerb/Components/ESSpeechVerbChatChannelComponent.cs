namespace Content.Shared._ES.Chat.SpeechVerb.Components;

[RegisterComponent]
public sealed partial class ESSpeechVerbChatChannelComponent : Component
{
    [DataField(required: true)]
    public LocId Format;
}
