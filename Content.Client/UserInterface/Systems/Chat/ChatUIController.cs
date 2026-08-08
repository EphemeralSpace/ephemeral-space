using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Client._ES.Chat;
using Content.Client.Administration.Managers;
using Content.Client.Chat;
using Content.Client.Chat.Managers;
using Content.Client.Chat.TypingIndicator;
using Content.Client.Chat.UI;
using Content.Client.Examine;
using Content.Client.Gameplay;
using Content.Client.Ghost;
using Content.Client.Mind;
using Content.Client.Roles;
using Content.Client.UserInterface.Systems.Chat.Widgets;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Shared._ES.Chat;
using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Chat;
using Content.Shared.Damage.ForceSay;
using Content.Shared.Input;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;
using Robust.Shared.Map;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;


namespace Content.Client.UserInterface.Systems.Chat;

public sealed partial class ChatUIController : UIController
{
    [Dependency] private IESChatManager _esChat = default!;
    [Dependency] private IClientAdminManager _admin = default!;
    [Dependency] private IChatManager _manager = default!;
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private IEyeManager _eye = default!;
    [Dependency] private IEntityManager _ent = default!;
    [Dependency] private IInputManager _input = default!;
    [Dependency] private IClientNetManager _net = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IStateManager _state = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private IReplayRecordingManager _replayRecording = default!;

    [UISystemDependency] private readonly ExamineSystem? _examine = default;
    [UISystemDependency] private readonly GhostSystem? _ghost = default;
    [UISystemDependency] private readonly TypingIndicatorSystem? _typingIndicator = default;
    [UISystemDependency] private readonly ChatSystem? _chatSys = default;
    [UISystemDependency] private readonly TransformSystem? _transform = default;
    [UISystemDependency] private readonly MindSystem? _mindSystem = default!;
    [UISystemDependency] private readonly RoleCodewordSystem? _roleCodewordSystem = default!;

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
    private readonly Dictionary<ChatChannel, int> _unreadMessages = new();

    // TODO add a cap for this for non-replays
    public readonly List<(GameTick Tick, ESChatMessage Msg)> History = new();

    // Maintains which channels a client should be able to filter (for showing in the chatbox)
    // and select (for attempting to send on).
    // This may not always actually match with what the server will actually allow them to
    // send / receive on, it is only what the user can select in the UI. For example,
    // if a user is silenced from speaking for some reason this may still contain ChatChannel.Local, it is left up
    // to the server to handle invalid attempts to use particular channels and not send messages for
    // channels the user shouldn't be able to hear.
    //
    // Note that Command is an available selection in the chatbox channel selector,
    // which is not actually a chat channel but is always available.
    public ChatSelectChannel CanSendChannels { get; private set; }
    public ChatChannel FilterableChannels { get; private set; }
    public ChatSelectChannel SelectableChannels { get; private set; }
    private ChatSelectChannel PreferredChannel { get; set; } = ChatSelectChannel.OOC;

    public event Action<ChatSelectChannel>? CanSendChannelsChanged;
    public event Action<ChatChannel>? FilterableChannelsChanged;
    public event Action<ChatSelectChannel>? SelectableChannelsChanged;
    public event Action<ChatChannel, int?>? UnreadMessageCountsUpdated;
    public event Action<ESChatMessage>? MessageAdded;

    public override void Initialize()
    {
        _sawmill = Logger.GetSawmill("chat");
        _sawmill.Level = LogLevel.Info;
        _admin.AdminStatusUpdated += UpdateChannelPermissions;
        _player.LocalPlayerAttached += OnAttachedChanged;
        _player.LocalPlayerDetached += OnAttachedChanged;
        _state.OnStateChanged += StateChanged;
        _net.RegisterNetMessage<MsgChatMessage>(OnChatMessage);
        _esChat.OnChatMessageSent += OnChatMessageSent;
        _net.RegisterNetMessage<MsgDeleteChatMessagesBy>(OnDeleteChatMessagesBy);
        SubscribeNetworkEvent<DamageForceSayEvent>(OnDamageForceSay);

        _speechBubbleRoot = new LayoutContainer();

        UpdateChannelPermissions();

        _input.SetInputCommand(ContentKeyFunctions.FocusChat,
            InputCmdHandler.FromDelegate(_ => FocusChat()));

        // TODO: doesn't support prototype reloading. TOO BAD!
        foreach (var chatChannel in _prototypeManager.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            if (chatChannel.FocusKey is not { } focusKey)
                return;

            _input.SetInputCommand(focusKey, InputCmdHandler.FromDelegate(_ => FocusChannel(chatChannel)));
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

    private void StateChanged(StateChangedEventArgs args)
    {
        if (args.NewState is GameplayState)
        {
            PreferredChannel = ChatSelectChannel.Local;
        }

        UpdateChannelPermissions();
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

    private void OnAttachedChanged(EntityUid uid)
    {
        UpdateChannelPermissions();
    }

    private void AddSpeechBubble(ESChatMessage msg, SpeechType speechType)
    {
        var ent = EntityManager.GetEntity(msg.Source);

        if (!EntityManager.EntityExists(ent))
        {
            _sawmill.Debug("Got local chat message with invalid sender entity: {0}", msg.Source);
            return;
        }

        EnqueueSpeechBubble(ent, msg, speechType);
    }

    private void CreateSpeechBubble(EntityUid entity, SpeechBubbleData speechData)
    {
        var bubble =
            SpeechBubble.CreateSpeechBubble(speechData.Type, speechData.Message, entity);

        bubble.OnDied += SpeechBubbleDied;

        if (_activeSpeechBubbles.TryGetValue(entity, out var existing))
        {
            // Push up existing bubbles above the mob's head.
            foreach (var existingBubble in existing)
            {
                existingBubble.VerticalOffset += bubble.ContentSize.Y;
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

        queueData.MessageQueue.Enqueue(new SpeechBubbleData(message, speechType));
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

    private void UpdateChannelPermissions()
    {
        CanSendChannels = default;
        FilterableChannels = default;

        // Can always send console stuff.
        CanSendChannels |= ChatSelectChannel.Console;

        // can always send/recieve OOC
        CanSendChannels |= ChatSelectChannel.OOC;
        CanSendChannels |= ChatSelectChannel.LOOC;
        FilterableChannels |= ChatChannel.OOC;
        FilterableChannels |= ChatChannel.LOOC;

        // can always hear server (nobody can actually send server messages).
        FilterableChannels |= ChatChannel.Server;

        if (_state.CurrentState is GameplayStateBase)
        {
            // can always hear local / radio / emote / notifications when in the game
            FilterableChannels |= ChatChannel.Local;
            FilterableChannels |= ChatChannel.Whisper;
            FilterableChannels |= ChatChannel.Radio;
            FilterableChannels |= ChatChannel.Emotes;
            FilterableChannels |= ChatChannel.Notifications;

            // Can only send local / radio / emote when attached to a non-ghost entity.
            // TODO: this logic is iffy (checking if controlling something that's NOT a ghost), is there a better way to check this?
            if (_ghost is not {IsGhost: true})
            {
                CanSendChannels |= ChatSelectChannel.Local;
                CanSendChannels |= ChatSelectChannel.Whisper;
                CanSendChannels |= ChatSelectChannel.Radio;
                CanSendChannels |= ChatSelectChannel.Emotes;
            }
        }

        // Only ghosts and admins can send / see deadchat.
        if (_admin.HasFlag(AdminFlags.Admin) || _ghost is {IsGhost: true})
        {
            FilterableChannels |= ChatChannel.Dead;
            CanSendChannels |= ChatSelectChannel.Dead;
        }

        // only admins can see / filter asay
        if (_admin.HasFlag(AdminFlags.Adminchat))
        {
            FilterableChannels |= ChatChannel.Admin;
            FilterableChannels |= ChatChannel.AdminAlert;
            FilterableChannels |= ChatChannel.AdminChat;
            CanSendChannels |= ChatSelectChannel.Admin;
        }

        SelectableChannels = CanSendChannels;

        // Necessary so that we always have a channel to fall back to.
        DebugTools.Assert((CanSendChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");
        DebugTools.Assert((FilterableChannels & ChatChannel.OOC) != 0, "OOC must always be available");
        DebugTools.Assert((SelectableChannels & ChatSelectChannel.OOC) != 0, "OOC must always be available");

        // let our chatbox know all the new settings
        CanSendChannelsChanged?.Invoke(CanSendChannels);
        FilterableChannelsChanged?.Invoke(FilterableChannels);
        SelectableChannelsChanged?.Invoke(SelectableChannels);
    }

    public void ClearUnfilteredUnreads(ChatChannel channels)
    {
        foreach (var channel in _unreadMessages.Keys.ToArray())
        {
            if ((channels & channel) == 0)
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

            queueData.TimeLeft += BubbleDelayBase + msg.Message.Content.Length * BubbleDelayFactor;

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
        var (prefixChannel, _) = SplitInputContents(box.ChatInput.Input.Text.ToLower());

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

        // TODO: radio is its own can of worms
        /*
        if (TryGetRadioChannel(text, out var radioChannel))
            chatChannel = ChatSelectChannel.Radio;
        */

        // TODO: re-evaluate after coding "can send message" stuff
        //if ((CanSendChannels & chatChannel) == 0)
        //    return (ChatSelectChannel.None, text, null);

        //if (chatChannel == ChatSelectChannel.Radio)
        //    return (chatChannel, text, radioChannel);

        return (chatChannel, trimmedText);
    }

    public void SendMessage(ChatBox box, ProtoId<ESChatChannelPrototype> channel)
    {
        _typingIndicator?.ClientSubmittedChatText();

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
        // Don't send on OOC/LOOC obviously!

        // TODO: GLORFCODE: Figure out which channels we send glorfmessages on
        // we need to handle selected channel
        // and prefix-channel separately..
        // var allowedChannels = ChatSelectChannel.Local | ChatSelectChannel.Whisper;
        // if ((chatBox.SelectedChannel & allowedChannels) == ChatSelectChannel.None)
        //     return;
        //
        // // none can be returned from this if theres no prefix,
        // // so we allow it in that case (assuming the previous check will have exited already if its an invalid channel)
        // var prefixChannel = SplitInputContents(msg).chatChannel;
        // if (prefixChannel != ChatSelectChannel.None && (prefixChannel & allowedChannels) == ChatSelectChannel.None)
        //     return;

        if (_player.LocalSession?.AttachedEntity is not { } ent
            || !EntityManager.TryGetComponent<DamageForceSayComponent>(ent, out var forceSay))
            return;

        if (string.IsNullOrWhiteSpace(msg))
            return;

        var modifiedText = ev.Suffix != null
            ? Loc.GetString(forceSay.ForceSayMessageWrap,
                ("message", msg), ("suffix", ev.Suffix))
            : Loc.GetString(forceSay.ForceSayMessageWrapNoSuffix,
                ("message", msg));

        chatBox.ChatInput.Input.SetText(modifiedText);
        chatBox.ChatInput.Input.ForceSubmitText();
    }

    private void OnChatMessage(MsgChatMessage message)
    {
        // no op
    }

    private void OnChatMessageSent(ESChatMessage msg)
    {
        ProcessChatMessage(msg);
    }

    public void ProcessChatMessage(ESChatMessage msg, bool speechBubble = true)
    {
        var channel = _prototypeManager.Index(msg.Channel);

        // color the name unless it's something like "the old man"
        // TODO: what the fuck.
        /*
        if ((msg.Channel == ChatChannel.Local || msg.Channel == ChatChannel.Whisper) && _chatNameColorsEnabled)
        {
            var grammar = _ent.GetComponentOrNull<GrammarComponent>(_ent.GetEntity(msg.SenderEntity));
            if (grammar != null && grammar.ProperNoun == true)
                msg.WrappedMessage = SharedChatSystem.InjectTagInsideTag(msg, "Name", "color", GetNameColor(SharedChatSystem.GetStringInsideTag(msg, "Name")).ToHex());
        }
        */

        // TODO: what the FUCK is this code. Do this shit serverside goddamn.
        /*
        // Color any codewords for minds that have roles that use them
        if (_player.LocalUser != null && _mindSystem != null && _roleCodewordSystem != null)
        {
            if (_mindSystem.TryGetMind(_player.LocalUser.Value, out var mindId) && _ent.TryGetComponent(mindId, out RoleCodewordComponent? codewordComp))
            {
                foreach (var (_, codewordData) in codewordComp.RoleCodewords)
                {
                    foreach (var codeword in codewordData.Codewords)
                    {
                        msg.WrappedMessage = SharedChatSystem.InjectTagAroundString(msg, codeword, "color", codewordData.Color.ToHex());
                    }
                }
            }
        }
        */

        // Log all incoming chat to repopulate when filter is un-toggled
        if (!msg.Ephemeral)
        {
            History.Add((_timing.CurTick, msg));
            MessageAdded?.Invoke(msg);

            if (!msg.Read)
            {
                // TODO restore
                /*
                _sawmill.Debug($"Message filtered: {msg.Channel}: {msg.FormattedMessage}");
                if (!_unreadMessages.TryGetValue(msg.Channel, out var count))
                    count = 0;

                count += 1;
                _unreadMessages[msg.Channel] = count;
                UnreadMessageCountsUpdated?.Invoke(msg.Channel, count);
                */
            }
        }

        // Local messages that have an entity attached get a speech bubble.
        if (!speechBubble || msg.Source == default || channel.SpeechBubbleType == SpeechType.None)
            return;

        AddSpeechBubble(msg, channel.SpeechBubbleType);

        /*
        switch (msg.Channel)
        {
            case ChatChannel.Local:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                break;

            case ChatChannel.Whisper:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Whisper);
                break;

            case ChatChannel.Dead:
                if (_ghost is not {IsGhost: true})
                    break;

                AddSpeechBubble(msg, SpeechBubble.SpeechType.Say);
                break;

            case ChatChannel.Emotes:
                AddSpeechBubble(msg, SpeechBubble.SpeechType.Emote);
                break;

            case ChatChannel.LOOC:
                if (_config.GetCVar(CCVars.LoocAboveHeadShow))
                    AddSpeechBubble(msg, SpeechBubble.SpeechType.Looc);
                break;

            // ES START
            case ChatChannel.OOC:
                // runlevel is uhh, not networked, otherwise i'd probably jsut check for
                // runlevel != ingame?
                // this is so chatbubbles show in the diegetic lobby, by the way
                if (UIManager.ActiveScreen is LobbyGui)
                    // could probably use a different styled speechbubble but I didn't have any great ideas with the
                    // current limitations of styling them.
                    AddSpeechBubble(msg, SpeechType.Say);
                break;
            // ES END
        }
        */
    }

    public void OnDeleteChatMessagesBy(MsgDeleteChatMessagesBy msg)
    {
        // This will delete messages from an entity even if different players were the author.
        // Usages of the erase admin verb should be rare enough that this does not matter.
        // Otherwise the client would need to know that one entity has multiple author players,
        // or the server would need to track when and which entities a player sent messages as.
        History.RemoveAll(h => h.Msg.SourceKey == msg.Key || msg.Entities.Contains(h.Msg.Source));
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

    public void NotifyChatTextChange()
    {
        _typingIndicator?.ClientChangedChatText();
    }

    public void NotifyChatFocus(bool isFocused)
    {
        _typingIndicator?.ClientChangedChatFocus(isFocused);
    }

    public void Repopulate()
    {
        foreach (var chat in _chats)
        {
            chat.Repopulate();
        }
    }

    private readonly record struct SpeechBubbleData(ESChatMessage Message, SpeechType Type);

    private sealed class SpeechBubbleQueueData
    {
        /// <summary>
        ///     Time left until the next speech bubble can appear.
        /// </summary>
        public float TimeLeft { get; set; }

        public Queue<SpeechBubbleData> MessageQueue { get; } = new();
    }
}
