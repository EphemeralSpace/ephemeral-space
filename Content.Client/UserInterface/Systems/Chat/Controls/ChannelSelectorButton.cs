using System.Numerics;
using Content.Shared._ES.Chat;
using Robust.Shared.Prototypes;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed class ChannelSelectorButton : ChatPopupButton<ChannelSelectorPopup>
{
    private readonly ChatUIController _chatUIController;

    public event Action<ProtoId<ESChatChannelPrototype>>? OnChannelSelect;

    public ProtoId<ESChatChannelPrototype> SelectedChannel { get; private set; } = ESSharedChatSystem.LocalChannel;

    private const int SelectorDropdownOffset = 38;

    public ChannelSelectorButton()
    {
        Name = "ChannelSelector";

        Popup.Selected += OnChannelSelected;

        _chatUIController = UserInterfaceManager.GetUIController<ChatUIController>();
        _chatUIController.LocalChatPermissionsUpdated += OnChatPermissionsUpdated;

        UpdateChannels();
    }

    private void OnChatPermissionsUpdated(EntityUid arg1, HashSet<ProtoId<ESChatChannelPrototype>> arg2)
    {
        UpdateChannels();
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

    public void UpdateChannels()
    {
        Popup.UpdateChannels(_chatUIController.GetPermittedChannels());
        if (Popup.FirstChannel is { } firstSelector &&
            !Popup.Channels.Contains(SelectedChannel))
        {
            Select(firstSelector);
        }
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

    public void UpdateChannelSelectButton(ESChatChannelPrototype channel)
    {
        Text = Loc.GetString(channel.Name);
        Modulate = channel.Color;
    }
}
