namespace Content.Shared.Chat;

public static class ChatChannelExtensions
{
    public static Color TextColor(this ChatChannel channel)
    {
        return channel switch
        {
            ChatChannel.Server => Color.Orange,
            ChatChannel.Radio => Color.LimeGreen,
            ChatChannel.LOOC => Color.MediumTurquoise,
            // ES START
            ChatChannel.OOC => Color.Plum,
            ChatChannel.Dead => Color.FromHex("#b9b9f8"),
            // ES END
            ChatChannel.Admin => Color.Red,
            ChatChannel.AdminAlert => Color.Red,
            ChatChannel.AdminChat => Color.HotPink,
            ChatChannel.Whisper => Color.DarkGray,
            _ => Color.LightGray
        };
    }
}
