using System.Diagnostics.CodeAnalysis;
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
        // TODO: prediction
        // No functionality on client.
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
