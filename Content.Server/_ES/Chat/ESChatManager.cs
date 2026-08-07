using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server._ES.Chat;

public sealed partial class ESChatManager : IESSharedChatManager
{
    [Dependency] private IEntityManager _entityManager = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private INetManager _netManager = default!;
    [Dependency] private ISharedPlayerManager _player = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IReplayRecordingManager _replayRecording = default!;

    public event Action<EntityUid, ESRequestSendChatMessage>? OnRequestSendChatMessage;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ESChatNetMessage>();
        _netManager.RegisterNetMessage<ESRequestSendChatNetMessage>(OnRequestSendChatNetMessage);
    }

    private void OnRequestSendChatNetMessage(ESRequestSendChatNetMessage message)
    {
        var session = _player.GetSessionByChannel(message.MsgChannel);

        // Should always have something attached
        if (session.AttachedEntity is not { } attachedEntity)
            return;

        OnRequestSendChatMessage?.Invoke(attachedEntity, message.Message);
    }

    public void SendChatMessage(
        string content,
        ICommonSession recipient,
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
        var channelPrototype = _prototype.Index(channel);

        var netSource = _entityManager.GetNetEntity(source);
        // TODO: derive sender key, reference ChatManager for impl

        var msg = new ESChatMessage(
            content,
            channel,
            netSource,
            null,
            ephemeral,
            sound,
            color ?? Color.White, // TODO: per-channel default color
            name,
            font,
            fontSize,
            format,
            _timing.CurTick
        );

        _netManager.ServerSendMessage(new ESChatNetMessage(msg), recipient.Channel);

        if (recordReplay && channelPrototype.SaveReplay)
        {
            _replayRecording.RecordServerMessage(msg);
        }
    }

    // TODO: i dont like this being duped across client and server but i cant be fucked to figure out the jank interface inheritance
    public bool TryGetChannelFromMessage(string content, [NotNullWhen(true)] out ESChatChannelPrototype? channel)
    {
        channel = null;

        content = content.Trim();
        if (content.Length == 0)
            return false;

        var c = content[0];
        foreach (var channelPrototype in _prototype.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            if (channelPrototype.Prefixes.Contains(c))
            {
                channel = channelPrototype;
                return true;
            }
        }

        return false;
    }
}
