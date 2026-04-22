using Content.Shared._ES.Chat;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Chat;

public sealed class ESChatManager : IESSharedChatManager
{
    public void Initialize()
    {

    }

    public void SendChatMessage(string content,
        ICommonSession recipient,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid source,
        string format,
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
