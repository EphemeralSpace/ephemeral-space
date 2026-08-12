using Content.Shared._ES.Chat;
using Robust.Client.UserInterface.Controls;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorItemButton : Button
{
    public const string StyleClassChatSelectorOptionButton = "ChatSelectorOptionButton";


    public readonly ESChatChannelPrototype Channel;

    public bool IsHidden => Parent == null;

    public ChannelSelectorItemButton(ESChatChannelPrototype selector)
    {
        Channel = selector;
        AddStyleClass(StyleClassChatSelectorOptionButton);

        Text = Loc.GetString(selector.Name);

        if (selector.TryGetDefaultPrefix(out var prefix))
            Text = Loc.GetString("hud-chatbox-select-name-prefixed", ("name", Text), ("prefix", prefix));

        Modulate = Channel.Color;
    }
}
