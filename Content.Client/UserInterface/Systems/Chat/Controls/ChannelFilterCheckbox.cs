using Content.Shared._ES.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelFilterCheckbox : CheckBox
{
    public readonly ESChatChannelFilterPrototype Channel;

    public bool IsHidden => Parent == null;

    public ChannelFilterCheckbox(ESChatChannelFilterPrototype channel)
    {
        Channel = channel;
        UpdateText(null);
    }

    private void UpdateText(int? unread)
    {
        var name = Loc.GetString(Channel.Name);

        if (unread > 0)
            // todo: proper fluent stuff here.
            name += " (" + (unread > 9 ? "9+" : unread) + ")";

        Text = name;
    }

    public void UpdateUnreadCount(int? unread)
    {
        UpdateText(unread);
    }
}
