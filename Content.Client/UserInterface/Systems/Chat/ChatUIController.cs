using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client._ES.Chat;
using Content.Client.Chat.TypingIndicator;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._ES.Chat;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Damage.ForceSay;
using Content.Shared.IdentityManagement;
using Content.Shared.Input;
using Robust.Client.Audio;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController : UIController, IOnSystemChanged<ESChatSystem>
{
    [Dependency] private IESChatManager _esChat = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IGameTiming _timing = default!;

    [UISystemDependency] private readonly AudioSystem? _audio = default!;
    [UISystemDependency] private readonly ExamineSystem? _examine = default;
    [UISystemDependency] private readonly TypingIndicatorSystem? _typingIndicator = default;
    [UISystemDependency] private readonly ESChatSystem? _chatSys = default;
    [UISystemDependency] private readonly TransformSystem? _transform = default;

    private ISawmill _sawmill = default!;

    /// <summary>
    ///     The max amount of chars allowed to fit in a single speech bubble.
    /// </summary>
    private const int SingleBubbleCharLimit = 100;

    /// <summary>
    ///     Base queue delay each speech bubble has.
    /// </summary>
    private const float BubbleDelayBase = 0.2f;

    /// <summary>
    ///     Factor multiplied by speech bubble char length to add to delay.
    /// </summary>
    private const float BubbleDelayFactor = 0.8f / SingleBubbleCharLimit;

    /// <summary>
    ///     The max amount of speech bubbles over a single entity at once.
    /// </summary>
    private const int SpeechBubbleCap = 4;

    private LayoutContainer _speechBubbleRoot = default!;

    /// <summary>
    ///     Speech bubbles that are currently visible on screen.
    ///     We track them to push them up when new ones get added.
    /// </summary>
    private readonly Dictionary<EntityUid, List<SpeechBubble>> _activeSpeechBubbles =
        new();

    /// <summary>
    ///     The speech bubble that is currently tied to the chatbox output.
    ///     i.e., when
    /// </summary>
    private SpeechBubble? _activeTypingSpeechBubble;

    /// <summary>
    ///     Speech bubbles that are to-be-sent because of the "rate limit" they have.
    /// </summary>
    private readonly Dictionary<EntityUid, SpeechBubbleQueueData> _queuedSpeechBubbles
        = new();

    private readonly HashSet<ChatBox> _chats = new();
    public IReadOnlySet<ChatBox> Chats => _chats;

    /// <summary>
    ///     The max amount of characters an entity can send in one message
    /// </summary>
    public int MaxMessageLength => _config.GetCVar(CCVars.ChatMaxMessageLength);

    /// <summary>
    /// For currently disabled chat filters,
    /// unread messages (messages received since the channel has been filtered out).
    /// </summary>
    private readonly Dictionary<ProtoId<ESChatChannelFilterPrototype>, int> _unreadMessages = new();

    // TODO add a cap for this for non-replays
    public readonly List<(GameTick Tick, ESChatMessage Msg)> History = new();

    public event Action<EntityUid, HashSet<ProtoId<ESChatChannelPrototype>>>? LocalChatPermissionsUpdated;
    public event Action<ProtoId<ESChatChannelFilterPrototype>, int?>? UnreadMessageCountsUpdated;
    public event Action<ESChatMessage>? MessageAdded;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
        _esChat.OnChatMessageSent += OnChatMessageSent;
        _net.RegisterNetMessage<MsgDeleteChatMessagesBy>(OnDeleteChatMessagesBy);
        SubscribeNetworkEvent<DamageForceSayEvent>(OnDamageForceSay);

        _speechBubbleRoot = new LayoutContainer();

        _input.SetInputCommand(ContentKeyFunctions.FocusChat,
            InputCmdHandler.FromDelegate(_ => FocusChat()));

        // TODO: doesn't support prototype reloading. TOO BAD!
        foreach (var chatChannel in _prototypeManager.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            if (chatChannel.Abstract)
                continue;

            if (chatChannel.FocusKey == null)
                continue;

            _input.SetInputCommand(chatChannel.FocusKey.Value, InputCmdHandler.FromDelegate(_ => FocusChannel(chatChannel)));
        }

        _input.SetInputCommand(ContentKeyFunctions.CycleChatChannelForward,
            InputCmdHandler.FromDelegate(_ => CycleChatChannel(true)));

        _input.SetInputCommand(ContentKeyFunctions.CycleChatChannelBackward,
            InputCmdHandler.FromDelegate(_ => CycleChatChannel(false)));

        var gameplayStateLoad = UIManager.GetUIController<GameplayStateLoadController>();
        gameplayStateLoad.OnScreenLoad += OnScreenLoad;
        gameplayStateLoad.OnScreenUnload += OnScreenUnload;

        _config.OnValueChanged(CCVars.ChatWindowOpacity, OnChatWindowOpacityChanged);
    }

    public void OnScreenLoad()
    {
        SetMainChat(true);

        var viewportContainer = UIManager.ActiveScreen!.FindControl<LayoutContainer>("ViewportContainer");
        SetSpeechBubbleRoot(viewportContainer);

        SetChatWindowOpacity(_config.GetCVar(CCVars.ChatWindowOpacity));

        if (_player.LocalEntity is { } local)
            OnLocalPermissionsUpdated(local, GetPermittedChannels());

        Repopulate();
    }

    public void OnScreenUnload()
    {
        SetMainChat(false);
    }

    private void OnChatWindowOpacityChanged(float opacity)
    {
        SetChatWindowOpacity(opacity);
    }

    private void SetChatWindowOpacity(float opacity)
    {
        // dude what the fuck is this code doing man
        var chatBox = UIManager.ActiveScreen?.GetWidget<ChatBox>();
        var stagehandChatBox = UIManager.ActiveScreen?.GetWidget<StagehandChatBox>();
        if (chatBox != null)
        {
            SetPanel(chatBox.ChatWindowPanel);
        }
        else if (stagehandChatBox != null)
        {
            SetPanel(stagehandChatBox.ChatWindowPanel);
            SetPanel(stagehandChatBox.StagehandChatWindowPanel);
        }

        void SetPanel(PanelContainer panel)
        {
            Color color;
            if (panel.PanelOverride is StyleBoxFlat styleBoxFlat)
                color = styleBoxFlat.BackgroundColor;
            else if (panel.TryGetStyleProperty<StyleBox>(PanelContainer.StylePropertyPanel, out var style)
                     && style is StyleBoxFlat propStyleBoxFlat)
                color = propStyleBoxFlat.BackgroundColor;
            else
                color = Color.FromHex("#25252ADD");

            panel.PanelOverride = new StyleBoxFlat
            {
                BackgroundColor = color.WithAlpha(opacity)
            };
        }
    }

    public void SetMainChat(bool setting)
    {
        if (UIManager.ActiveScreen is null)
        {
            return;
        }

        var chatBox = UIManager.GetActiveUIWidgetOrNull<ChatBox>() ??
                      UIManager.GetActiveUIWidgetOrNull<StagehandChatBox>();

        if (chatBox == null)
        {
            Log.Error($"Could not find chatbox in ingame screen {UIManager.ActiveScreen.GetType().Name}!");
            return;
        }

        chatBox.Main = setting;
    }

    public bool TryGetMainChat([NotNullWhen(true)] out ChatBox? chat)
    {
        foreach (var c in _chats)
        {
            if (!c.Main)
                continue;
            chat = c;
            return true;
        }

        chat = null;
        return false;
    }

    private void FocusChat()
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.Focus();
            break;
        }
    }

    private void FocusChannel(ProtoId<ESChatChannelPrototype> channel)
    {
        if (TryGetMainChat(out var chat))
            chat.Focus(channel);
    }

    private void CycleChatChannel(bool forward)
    {
        foreach (var chat in _chats)
        {
            if (!chat.Main)
                continue;

            chat.CycleChatChannel(forward);
            break;
        }
    }

    public void SetSpeechBubbleRoot(LayoutContainer root)
    {
        _speechBubbleRoot.Orphan();
        root.AddChild(_speechBubbleRoot);
        LayoutContainer.SetAnchorPreset(_speechBubbleRoot, LayoutContainer.LayoutPreset.Wide);
        // todo make the speech bubble container an actual uiwidget in the game screens instead of doing this dumb shit
        _speechBubbleRoot.SetPositionInParent(root.ChildCount - 2);
        _speechBubbleRoot.RectClipContent = true;
    }

    private void AddSpeechBubble(ESChatMessage msg, SpeechType speechType)
    {
        if (msg.Source == null)
            return;

        if (!EntityManager.TryGetEntity(msg.Source.Value, out var ent) ||
            !EntityManager.EntityExists(ent))
            return;

        EnqueueSpeechBubble(ent.Value, msg, speechType);
    }

    private SpeechBubble CreateSpeechBubble(EntityUid entity, SpeechBubbleData speechData)
    {
        var bubble =
            SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Name, speechData.Content, entity);

        bubble.OnDied += SpeechBubbleDied;

        if (_activeSpeechBubbles.TryGetValue(entity, out var existing))
        {
            var last = existing.Last();
            // dont push bubbles up if we're creating a new bubble in place of one thats fading
            // (this most commonly happens with the active typing speech bubble)
            if (last.Modulate.A >= 1f && !last.Fading)
            {
                // Push up existing bubbles above the mob's head.
                foreach (var existingBubble in existing)
                {
                    existingBubble.VerticalOffset += bubble.ContentSize.Y;
                }
            }
        }
        else
        {
            existing = new List<SpeechBubble>();
            _activeSpeechBubbles.Add(entity, existing);
        }

        existing.Add(bubble);
        _speechBubbleRoot.AddChild(bubble);

        if (existing.Count > SpeechBubbleCap)
        {
            // Get the next speech bubble to fade
            // Any speech bubbles before it are already fading
            var last = existing[^(SpeechBubbleCap + 1)];
            last.FadeNow();
        }

        return bubble;
    }

    private void SpeechBubbleDied(EntityUid entity, SpeechBubble bubble)
    {
        RemoveSpeechBubble(entity, bubble);
    }

    private void EnqueueSpeechBubble(EntityUid entity, ESChatMessage message, SpeechType speechType)
    {
        // Don't enqueue speech bubbles for other maps. TODO: Support multiple viewports/maps?
        if (EntityManager.GetComponent<TransformComponent>(entity).MapID != _eye.CurrentEye.Position.MapId)
            return;

        if (!_queuedSpeechBubbles.TryGetValue(entity, out var queueData))
        {
            queueData = new SpeechBubbleQueueData();
            _queuedSpeechBubbles.Add(entity, queueData);
        }

        queueData.MessageQueue.Enqueue(new SpeechBubbleData(message.Name, message.Content, speechType));
    }

    public void RemoveSpeechBubble(EntityUid entityUid, SpeechBubble bubble)
    {
        bubble.Dispose();

        var list = _activeSpeechBubbles[entityUid];
        list.Remove(bubble);

        if (list.Count == 0)
        {
            _activeSpeechBubbles.Remove(entityUid);
        }
    }

    public void ClearUnfilteredUnreads(ProtoId<ESChatChannelFilterPrototype> filterChannel)
    {
        foreach (var channel in _unreadMessages.Keys.ToArray())
        {
            if (channel != filterChannel)
                continue;

            _unreadMessages[channel] = 0;
            UnreadMessageCountsUpdated?.Invoke(channel, 0);
        }
    }

    public override void FrameUpdate(FrameEventArgs delta)
    {
        UpdateQueuedSpeechBubbles(delta);
    }

    private void UpdateQueuedSpeechBubbles(FrameEventArgs delta)
    {
        // Update queued speech bubbles.
        if (_queuedSpeechBubbles.Count == 0 || _examine == null)
        {
            return;
        }

        foreach (var (entity, queueData) in _queuedSpeechBubbles.ShallowClone())
        {
            if (!EntityManager.EntityExists(entity))
            {
                _queuedSpeechBubbles.Remove(entity);
                continue;
            }

            queueData.TimeLeft -= delta.DeltaSeconds;
            if (queueData.TimeLeft > 0)
            {
                continue;
            }

            if (queueData.MessageQueue.Count == 0)
            {
                _queuedSpeechBubbles.Remove(entity);
                continue;
            }

            var msg = queueData.MessageQueue.Dequeue();

            queueData.TimeLeft += BubbleDelayBase + msg.Content.Length * BubbleDelayFactor;

            // We keep the queue around while it has 0 items. This allows us to keep the timer.
            // When the timer hits 0 and there's no messages left, THEN we can clear it up.
            CreateSpeechBubble(entity, msg);
        }

        var player = _player.LocalEntity;
        var predicate = static (EntityUid uid, (EntityUid compOwner, EntityUid? attachedEntity) data)
            => uid == data.compOwner || uid == data.attachedEntity;
        var playerPos = player != null
            ? _eye.CurrentEye.Position
            : MapCoordinates.Nullspace;

        var occluded = player != null && _examine.IsOccluded(player.Value);

        foreach (var (ent, bubs) in _activeSpeechBubbles)
        {
            if (EntityManager.Deleted(ent))
            {
                SetBubbles(bubs, false);
                continue;
            }

            if (ent == player)
            {
                SetBubbles(bubs, true);
                continue;
            }

            var otherPos = _transform?.GetMapCoordinates(ent) ?? MapCoordinates.Nullspace;

            if (occluded && !_examine.InRangeUnOccluded(
                    playerPos,
                    otherPos, 0f,
                    (ent, player), predicate))
            {
                SetBubbles(bubs, false);
                continue;
            }

            SetBubbles(bubs, true);
        }
    }

    private void SetBubbles(List<SpeechBubble> bubbles, bool visible)
    {
        foreach (var bubble in bubbles)
        {
            bubble.Visible = visible;
        }
    }

    public void UpdateSelectedChannel(ChatBox box)
    {
        var (prefixChannel, _) = SplitInputContents(box.ChatInput.Input.Text);

        if (prefixChannel == null)
            box.ChatInput.ChannelSelector.UpdateChannelSelectButton(_prototypeManager.Index(box.SelectedChannel));
        else
            box.ChatInput.ChannelSelector.UpdateChannelSelectButton(prefixChannel);
    }

    public (ESChatChannelPrototype? chatChannel, string text) SplitInputContents(string text)
    {
        text = text.Trim();
        if (text.Length == 0)
            return (null, text);

        if (!_esChat.TryGetChannelFromMessage(text, out var chatChannel, out var trimmedText))
            return (null, text);

        if (!GetPermittedChannels().Contains(chatChannel))
            return (null, text);

        return (chatChannel, trimmedText);
    }

    public void SendMessage(ChatBox box, ProtoId<ESChatChannelPrototype> channel)
    {
        _typingIndicator?.ClientSubmittedChatText();
        _activeTypingSpeechBubble?.FadeNow();
        _activeTypingSpeechBubble = null;

        var text = box.ChatInput.Input.Text;
        box.ChatInput.Input.Clear();
        box.ChatInput.Input.ReleaseKeyboardFocus();
        UpdateSelectedChannel(box);

        if (string.IsNullOrWhiteSpace(text))
            return;

        (var prefixChannel, text) = SplitInputContents(text);

        // Check if message is longer than the character limit
        if (text.Length > MaxMessageLength)
        {
            var locWarning = Loc.GetString("chat-manager-max-message-length",
                ("maxMessageLength", MaxMessageLength));
            box.AddLine(locWarning, Color.Orange);
            return;
        }

        if (prefixChannel != null)
            channel = prefixChannel;

        _esChat.RequestSendChatMessage(text, channel);
    }

    private void OnDamageForceSay(DamageForceSayEvent ev, EntitySessionEventArgs _)
    {
        var chatBox = UIManager.ActiveScreen?.GetWidget<ChatBox>() ?? UIManager.ActiveScreen?.GetWidget<StagehandChatBox>();
        if (chatBox == null)
            return;

        var msg = chatBox.ChatInput.Input.Text.TrimEnd();
        var prefixChannel = SplitInputContents(msg).chatChannel;
        prefixChannel ??= _prototypeManager.Index(chatBox.SelectedChannel);

        if (!prefixChannel.GlorfAffected)
            return;

        if (_player.LocalSession?.AttachedEntity is not { } ent
            || !EntityManager.TryGetComponent<DamageForceSayComponent>(ent, out var forceSay))
            return;

        if (string.IsNullOrWhiteSpace(msg))
            return;

        var modifiedText = ev.Suffix != null
            ? Loc.GetString(forceSay.ForceSayMessageWrap,
                ("message", msg),
                ("suffix", ev.Suffix))
            : Loc.GetString(forceSay.ForceSayMessageWrapNoSuffix,
                ("message", msg));

        chatBox.ChatInput.Input.SetText(modifiedText);
        chatBox.ChatInput.Input.ForceSubmitText();
    }

    /// <summary>
    ///     Creates or updates a speechbubble for the current entity containing the contents of the current chat input.
    /// </summary>
    public void TryUpdateTypingSpeechBubble(string text, ProtoId<ESChatChannelPrototype> channel, bool forceRebuild = false)
    {
        if (_player.LocalEntity is not { } entity)
            return;

        if (!_prototypeManager.TryIndex(channel, out var proto))
            return;

        var bubbleType = proto.SpeechBubbleType ?? SpeechType.Say;

        if (forceRebuild && _activeTypingSpeechBubble is not null)
        {
            _activeTypingSpeechBubble.FadeNow();
            _activeTypingSpeechBubble = null;
        }

        if (_activeTypingSpeechBubble is not null && !_activeTypingSpeechBubble.Disposed)
        {
            _activeTypingSpeechBubble.RebuildBubbleContents(_activeTypingSpeechBubble.NameText, text, bubbleType);
        }
        else
        {
            _activeTypingSpeechBubble =
                CreateSpeechBubble(entity, new SpeechBubbleData(Identity.Name(entity, EntityManager), text, bubbleType));
            _activeTypingSpeechBubble.MakePermanent();
        }
    }

    private void OnChatMessageSent(ESChatMessage msg)
    {
        ProcessChatMessage(msg);
    }

    public void ProcessChatMessage(ESChatMessage msg, bool speechBubble = true)
    {
        var channel = _prototypeManager.Index(msg.Channel);

        // Log all incoming chat to repopulate when filter is un-toggled
        if (!msg.Ephemeral)
        {
            History.Add((_timing.CurTick, msg));
            MessageAdded?.Invoke(msg);

            if (!msg.Read)
            {
                _sawmill.Debug($"Message filtered: {msg.Channel}: {msg.FormattedMessage}");
                var count = _unreadMessages.GetValueOrDefault(channel.FilterCategory, 0);

                count += 1;
                _unreadMessages[channel.FilterCategory] = count;
                UnreadMessageCountsUpdated?.Invoke(channel.FilterCategory, count);
            }
            else
            {
                // Only play audio if the message was actively read.
                _audio?.PlayGlobal(msg.Sound, Filter.Local(), false);
            }
        }

        // Local messages that have an entity attached get a speech bubble.
        if (!speechBubble || msg.Source == default || !channel.SpeechBubbleType.HasValue)
            return;

        AddSpeechBubble(msg, channel.SpeechBubbleType.Value);
    }

    public void OnDeleteChatMessagesBy(MsgDeleteChatMessagesBy msg)
    {
        // This will delete messages from an entity even if different players were the author.
        // Usages of the erase admin verb should be rare enough that this does not matter.
        // Otherwise the client would need to know that one entity has multiple author players,
        // or the server would need to track when and which entities a player sent messages as.
        History.RemoveAll(h =>
        {
            if (h.Msg.SourceKey == msg.Key)
                return true;

            if (h.Msg.Source is { } source &&
                msg.Entities.Contains(source))
                return true;

            return false;
        });
        Repopulate();
    }

    public void RegisterChat(ChatBox chat)
    {
        _chats.Add(chat);
    }

    public void UnregisterChat(ChatBox chat)
    {
        _chats.Remove(chat);
    }

    public void NotifyChatTextChange(ChatBox box)
    {
        var channel = GetCurrentChatChannel(box);
        _typingIndicator?.ClientChangedValidChannel(channel);
        _typingIndicator?.ClientChangedChatText();
        TryUpdateTypingSpeechBubble(box.ChatInput.Input.Text, channel);
    }

    public void NotifyChatFocus(ChatBox box, bool isFocused)
    {
        _typingIndicator?.ClientChangedChatFocus(isFocused);

        if (!isFocused)
        {
            _activeTypingSpeechBubble?.FadeNow();
            _activeTypingSpeechBubble = null;
        }
    }

    public void NotifyChatSelectorChanged(ChatBox box)
    {
        UpdateSelectedChannel(box);
        if (box.ChatInput.Input.Text != string.Empty)
            TryUpdateTypingSpeechBubble(box.ChatInput.Input.Text, box.SelectedChannel, true);
    }

    private ESChatChannelPrototype GetCurrentChatChannel(ChatBox box)
    {
        var (channel, _) = SplitInputContents(box.ChatInput.Input.Text);
        if (channel != null)
            return channel;

        return _prototypeManager.Index(box.SelectedChannel);
    }

    public void Repopulate()
    {
        foreach (var chat in _chats)
        {
            chat.Repopulate();
        }
    }

    private readonly record struct SpeechBubbleData(string Name, string Content, SpeechType Type);

    private sealed class SpeechBubbleQueueData
    {
        /// <summary>
        ///     Time left until the next speech bubble can appear.
        /// </summary>
        public float TimeLeft { get; set; }

        public Queue<SpeechBubbleData> MessageQueue { get; } = new();
    }

    public void OnSystemLoaded(ESChatSystem system)
    {
        system.LocalChatPermissionsUpdated += OnLocalPermissionsUpdated;
        system.ChatChannelFocused += OnChatChannelFocused;
    }

    public void OnSystemUnloaded(ESChatSystem system)
    {
        system.LocalChatPermissionsUpdated -= OnLocalPermissionsUpdated;
        system.ChatChannelFocused -= OnChatChannelFocused;
    }

    private void OnLocalPermissionsUpdated(EntityUid uid, HashSet<ProtoId<ESChatChannelPrototype>> channels)
    {
        LocalChatPermissionsUpdated?.Invoke(uid, channels);
    }

    private void OnChatChannelFocused(ProtoId<ESChatChannelPrototype> channel)
    {
        if (TryGetMainChat(out var box))
            box.Focus(channel);
    }

    public HashSet<ProtoId<ESChatChannelPrototype>> GetPermittedChannels()
    {
        if (_player.LocalEntity == null || _chatSys == null)
            return [ ESSharedChatSystem.LocalChannel ];
        return _chatSys.GetPermittedChannels(_player.LocalEntity.Value);
    }
}
