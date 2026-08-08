using Content.Shared._ES.Chat;
using Content.Shared.CCVar;
using Content.Shared.Radio;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;

namespace Content.Client._ES.Chat;

public sealed partial class ESChatManager : ESSharedChatManager, IESChatManager
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IReplayRecordingManager _replayRecording = default!;

    public event Action<ESChatMessage>? OnChatMessageSent;

    public override void Initialize()
    {
        base.Initialize();

        _net.RegisterNetMessage<ESChatNetMessage>(OnChatNetMessage);
        _net.RegisterNetMessage<ESRequestSendChatNetMessage>();
    }

    private void OnChatNetMessage(ESChatNetMessage message)
    {
        var msg = message.Message;
        OnChatMessageSent?.Invoke(msg);

        if (PrototypeManager.Index(msg.Channel).SaveReplay &&
            _config.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replayRecording.RecordClientMessage(msg);
        }
    }

    public override void SendChatMessage(string content,
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
        // No functionality on client.
    }

    public void RequestSendChatMessage(string message, ProtoId<ESChatChannelPrototype> channel)
    {
        var msg = new ESRequestSendChatMessage(message, channel);

        _net.ClientSendMessage(new ESRequestSendChatNetMessage(msg));
        // TODO: prediction
    }
}
