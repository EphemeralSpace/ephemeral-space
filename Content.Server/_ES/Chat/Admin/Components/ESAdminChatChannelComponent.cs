namespace Content.Server._ES.Chat.Admin.Components;

/// <summary>
/// Used for chat channels which are only relayed to admins.
/// </summary>
[RegisterComponent]
[Access(typeof(ESAdminChatChannelSystem))]
public sealed partial class ESAdminChatChannelComponent : Component
{
    /// <summary>
    /// If true, admins can also send directly on this channel
    /// </summary>
    [DataField]
    public bool AdminSendable;
}
