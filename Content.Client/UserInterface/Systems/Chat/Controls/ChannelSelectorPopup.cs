using System.Linq;
using Content.Shared._ES.Chat;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;
using static Robust.Client.UserInterface.Controls.BaseButton;

namespace Content.Client.UserInterface.Systems.Chat.Controls;

public sealed partial class ChannelSelectorPopup : Popup
{
    [Dependency] private IPrototypeManager _prototype = default!;

    private readonly BoxContainer _channelSelectorHBox;
    private readonly Dictionary<ProtoId<ESChatChannelPrototype>, ChannelSelectorItemButton> _selectorStates = new();

    public event Action<ProtoId<ESChatChannelPrototype>>? Selected;

    public List<ProtoId<ESChatChannelPrototype>> Channels { get; } = new();

    public ProtoId<ESChatChannelPrototype>? FirstChannel => Channels.FirstOrNull();

    public ChannelSelectorPopup()
    {
        IoCManager.InjectDependencies(this);

        _channelSelectorHBox = new BoxContainer
        {
            Orientation = BoxContainer.LayoutOrientation.Horizontal,
            SeparationOverride = 1
        };

        AddChild(_channelSelectorHBox);
    }

    public void UpdateChannels(HashSet<ProtoId<ESChatChannelPrototype>> channels)
    {
        Channels.Clear();
        _channelSelectorHBox.RemoveAllChildren();

        var order = _prototype.Index(IESSharedChatManager.ChannelOrder);
        foreach (var channel in _prototype.EnumeratePrototypes<ESChatChannelPrototype>().OrderBy(p => order.Order.IndexOf(p)))
        {
            if (channel.Abstract)
                continue;

            if (!_selectorStates.TryGetValue(channel, out var selector))
            {
                selector = new ChannelSelectorItemButton(channel);
                _selectorStates.Add(channel, selector);
                selector.OnPressed += OnSelectorPressed;
            }

            if (!channels.Contains(channel))
            {
                if (selector.Parent == _channelSelectorHBox)
                {
                    _channelSelectorHBox.RemoveChild(selector);
                }
            }
            else if (selector.IsHidden)
            {
                Channels.Add(channel.ID);
                _channelSelectorHBox.AddChild(selector);
            }
        }
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
}
