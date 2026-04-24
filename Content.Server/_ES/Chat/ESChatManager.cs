using Content.Shared._ES.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;
using Robust.Shared.Timing;

namespace Content.Server._ES.Chat;

public sealed class ESChatManager : IESSharedChatManager
{
    [Dependency] private readonly IEntityManager _entityManager = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _netManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IReplayRecordingManager _replayRecording = default!;

    public void Initialize()
    {
        _netManager.RegisterNetMessage<ESChatNetMessage>();
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
            color ?? Color.White,
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
}
