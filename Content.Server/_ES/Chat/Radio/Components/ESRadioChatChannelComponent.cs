namespace Content.Server._ES.Chat.Radio.Components;

[RegisterComponent]
public sealed partial class ESRadioChatChannelComponent : Component
{
    [DataField]
    public bool RequireServer;
}
