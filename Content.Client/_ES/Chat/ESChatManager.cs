using Content.Shared._ES.Chat;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Replays;

namespace Content.Client._ES.Chat;

public sealed partial class ESChatManager : IESChatManager
{
    [Dependency] private IConfigurationManager _config = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IReplayRecordingManager _replayRecording = default!;

    public event Action<ESChatMessage>? OnChatMessageSent;

    public void Initialize()
    {
        _net.RegisterNetMessage<ESChatNetMessage>(OnChatNetMessage);
    }

    private void OnChatNetMessage(ESChatNetMessage message)
    {
        var msg = message.Message;
        OnChatMessageSent?.Invoke(msg);

        if (_prototype.Index(msg.Channel).SaveReplay &&
            _config.GetCVar(CCVars.ReplayRecordAdminChat))
        {
            _replayRecording.RecordClientMessage(msg);
        }
    }

    public void SendChatMessage(string content,
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
}
