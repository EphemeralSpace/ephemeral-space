using Content.Shared._ES.Chat;
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
    }

    private void OnRequestSendChatNetMessage(ESRequestSendChatNetMessage message)
    {
        var session = _player.GetSessionByChannel(message.MsgChannel);

        // TODO: Generic ratelimiting

        // Protect against bad messages
        if (!PrototypeManager.HasIndex(message.Message.ChatChannel))
            return;

        // Should always have something attached
        if (session.AttachedEntity is not { } attachedEntity)
            return;

        if (message.Message.Text.Length > MaxMessageLength)
            return;

        var content = FormattedMessage.EscapeText(message.Message.Text.Trim());
        InvokeRequestSendChatMessage(attachedEntity, content, message.Message.ChatChannel);
    }

    public override void SendChatMessage(
        string content,
        IEnumerable<ICommonSession> recipients,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid source,
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

        var netSource = _entityManager.GetNetEntity(source);
        // TODO: derive sender key, reference ChatManager for impl

        var msg = new ESChatMessage(
            content,
            channel,
            netSource,
            null,
            ephemeral,
            sound,
            color ?? channelPrototype.Color,
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
}
