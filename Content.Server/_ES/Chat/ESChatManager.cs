using System.Linq;
using System.Runtime.InteropServices;
using Content.Server.Administration.Managers;
using Content.Server.Chat;
using Content.Shared._ES.Chat;
using Content.Shared.Administration;
using Content.Shared.Chat;
using Content.Shared.Players.RateLimiting;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Server._ES.Chat;

public sealed partial class ESChatManager : ESSharedChatManager
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IReplayRecordingManager _replayRecording = default!;

    public override void Initialize()
    {
        base.Initialize();

        _netManager.RegisterNetMessage<ESChatNetMessage>();
        _netManager.RegisterNetMessage<ESRequestSendChatNetMessage>(OnRequestSendChatNetMessage);

        RegisterRateLimits();
    }

    private void OnRequestSendChatNetMessage(ESRequestSendChatNetMessage message)
    {
        var session = _player.GetSessionByChannel(message.MsgChannel);

        if (HandleRateLimit(session) == RateLimitStatus.Blocked)
            return;

        // Protect against bad messages
        if (!PrototypeManager.HasIndex(message.Message.ChatChannel))
            return;

        // Should always have something attached
        if (session.AttachedEntity is not { } attachedEntity)
            return;

        if (message.Message.Text.Length > MaxMessageLength)
        {
            var feedback = Loc.GetString("chat-manager-max-message-length-exceeded-message", ("limit", MaxMessageLength));
            SendServerMessage(feedback, session);
            return;
        }

        var content = FormattedMessage.EscapeText(message.Message.Text.Trim());
        InvokeRequestSendChatMessage(attachedEntity, content, message.Message.ChatChannel);
    }

    public override void SendAdminMessage(string content, AdminFlags? flagBlacklist = null, AdminFlags? flagWhitelist = null)
    {
        var clients = _admin.ActiveAdmins.Where(p =>
        {
            var adminData = _admin.GetAdminData(p);

            DebugTools.AssertNotNull(adminData);

            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (adminData == null)
                return false;

            if (flagBlacklist != null && adminData.HasFlag(flagBlacklist.Value))
                return false;

            return flagWhitelist == null || adminData.HasFlag(flagWhitelist.Value);
        });
        SendAdminMessage(content, clients);
    }

    public override void SendChatMessage(
        string content,
        IEnumerable<ICommonSession> recipients,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid? source,
        string format = IESSharedChatManager.DefaultFormat,
        bool ephemeral = false,
        bool recordReplay = true,
        SoundSpecifier? sound = null,
        Color? color = null,
        string? name = null,
        string? font = null,
        int? fontSize = null)
    {
        var channelPrototype = PrototypeManager.Index(channel);

        // Get a per-user key for tracking messages
        var user = EnsurePlayer(source);
        var netSource = _entityManager.GetNetEntity(source);
        if (netSource.HasValue)
            user?.AddEntity(netSource.Value);

        var msg = new ESChatMessage(
            content,
            channel,
            netSource,
            user?.Key,
            ephemeral,
            sound,
            color ?? channelPrototype.TextColor,
            name,
            font,
            fontSize,
            format,
            _timing.CurTick
        );

        foreach (var recipient in recipients)
        {
            _netManager.ServerSendMessage(new ESChatNetMessage(msg), recipient.Channel);
        }

        if (recordReplay && channelPrototype.SaveReplay)
        {
            _replayRecording.RecordServerMessage(msg);
        }

        // DISCORD LINK
    }

    private readonly Dictionary<NetUserId, ChatUser> _players = new();

    public override void DeleteMessagesBy(NetUserId uid)
    {
        if (!_players.TryGetValue(uid, out var user))
            return;

        var msg = new MsgDeleteChatMessagesBy { Key = user.Key, Entities = user.Entities };
        _netManager.ServerSendToAll(msg);
    }

    public ChatUser? EnsurePlayer(EntityUid? source)
    {
        if (source == null)
            return null;

        if (!_player.TryGetSessionByEntity(source.Value, out var session))
            return null;

        var author = session.UserId;

        ref var user = ref CollectionsMarshal.GetValueRefOrAddDefault(_players, author, out var exists);
        if (!exists || user == null)
            user = new ChatUser(_players.Count);

        return user;
    }
}
