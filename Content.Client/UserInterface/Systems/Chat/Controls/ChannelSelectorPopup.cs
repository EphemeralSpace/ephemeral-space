using System.Linq;
using Content.Shared._ES.Chat;
using Content.Shared.Chat;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed partial class ChannelSelectorPopup : Popup
{
    [Dependency] private IPrototypeManager _prototype = default!;

    public List<ProtoId<ESChatChannelPrototype>> Channels = new();

    private readonly BoxContainer _channelSelectorHBox;
    private readonly Dictionary<ProtoId<ESChatChannelPrototype>, ChannelSelectorItemButton> _selectorStates = new();
    private readonly ChatUIController _chatUIController;

    public event Action<ProtoId<ESChatChannelPrototype>>? Selected;

    public ChannelSelectorPopup()
    {
        IoCManager.InjectDependencies(this);

        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };

        _chatUIController = UserInterfaceManager.GetUIController<ChatUIController>();
        _chatUIController.SelectableChannelsChanged += SetChannels;
        SetChannels(_chatUIController.SelectableChannels);

        AddChild(_channelSelectorHBox);
    }

    public ProtoId<ESChatChannelPrototype>? FirstChannel
    {
        get
        {
            foreach (var selector in _selectorStates.Values)
            {
                if (!selector.IsHidden)
                    return selector.Channel;
            }

            return null;
        }
    }

    public void SetChannels(ChatSelectChannel channels)
    {
        //var wasPreferredAvailable = IsPreferredAvailable();

        Channels.Clear();
        _channelSelectorHBox.RemoveAllChildren();

        foreach (var channel in _prototype.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            if (!_selectorStates.TryGetValue(channel, out var selector))
            {
                selector = new ChannelSelectorItemButton(channel);
                _selectorStates.Add(channel, selector);
                selector.OnPressed += OnSelectorPressed;
            }

            Channels.Add(channel.ID);

            _channelSelectorHBox.AddChild(selector);

            // TODO: FUCK IT!!!
            // if ((channels & channel) == 0)
            // {
            //     if (selector.Parent == _channelSelectorHBox)
            //     {
            //         _channelSelectorHBox.RemoveChild(selector);
            //     }
            // }
            // else if (selector.IsHidden)
            // {
            //     _channelSelectorHBox.AddChild(selector);
            // }
        }

        if (Channels.FirstOrDefault() is { } first)
            Select(first);

        // TODO: figure out preferred channel logic
        // var isPreferredAvailable = IsPreferredAvailable();
        // if (!wasPreferredAvailable && isPreferredAvailable)
        // {
        //     Select(_chatUIController.GetPreferredChannel());
        // }
        // else if (wasPreferredAvailable && !isPreferredAvailable)
        // {
        //     Select(ChatSelectChannel.OOC);
        // }
    }

    private void OnSelectorPressed(ButtonEventArgs args)
    {
        var button = (ChannelSelectorItemButton) args.Button;
        Select(button.Channel);
    }

    private void Select(ProtoId<ESChatChannelPrototype> channel)
    {
        Selected?.Invoke(channel);
    }

    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);

        if (!disposing)
            return;

        _chatUIController.SelectableChannelsChanged -= SetChannels;
    }
}
