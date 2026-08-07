using System.Numerics;
using Content.Shared._ES.Chat;
using Content.Shared.Chat;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorButton : ChatPopupButton<ChannelSelectorPopup>
{
    public event Action<ProtoId<ESChatChannelPrototype>>? OnChannelSelect;

    public ProtoId<ESChatChannelPrototype> SelectedChannel { get; private set; }

    private const int SelectorDropdownOffset = 38;

    public ChannelSelectorButton()
    {
        Name = "ChannelSelector";

        Popup.Selected += OnChannelSelected;

        if (Popup.FirstChannel is { } firstSelector)
        {
            Select(firstSelector);
        }
    }

    protected override UIBox2 GetPopupPosition()
    {
        var globalLeft = GlobalPosition.X;
        var globalBot = GlobalPosition.Y + Height;
        return UIBox2.FromDimensions(
            new Vector2(globalLeft, globalBot),
            new Vector2(SizeBox.Width, SelectorDropdownOffset));
    }

    private void OnChannelSelected(ProtoId<ESChatChannelPrototype> channel)
    {
        Select(channel);
    }

    public void Select(ProtoId<ESChatChannelPrototype> channel)
    {
        if (Popup.Visible)
        {
            Popup.Close();
        }

        if (SelectedChannel == channel)
            return;
        SelectedChannel = channel;
        OnChannelSelect?.Invoke(channel);
    }

    public static string ChannelSelectorName(ChatSelectChannel channel)
    {
        return Loc.GetString($"hud-chatbox-select-channel-{channel}");
    }

    public Color ChannelSelectColor(ChatSelectChannel channel)
    {
        return channel switch
        {
            ChatSelectChannel.Radio => Color.LimeGreen,
            ChatSelectChannel.LOOC => Color.MediumTurquoise,
            // ES START
            ChatSelectChannel.OOC => Color.Plum,
            ChatSelectChannel.Dead => Color.FromHex("#b9b9f8"),
            // ES END
            ChatSelectChannel.Admin => Color.HotPink,
            _ => Color.DarkGray
        };
    }

    public void UpdateChannelSelectButton(ESChatChannelPrototype channel, RadioChannelPrototype? radio)
    {
        Text = radio != null ? Loc.GetString(radio.Name) : Loc.GetString(channel.Name);
        Modulate = radio?.Color ?? channel.Color;
    }
}
