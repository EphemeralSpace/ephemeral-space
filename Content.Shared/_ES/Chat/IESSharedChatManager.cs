using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public interface IESSharedChatManager
{
    const string DefaultFormat = "{0}";

    event Action<EntityUid, ESRequestSendChatMessage>? OnRequestSendChatMessage;

    void Initialize();

    void SendChatMessage(
        string content,
        ICommonSession recipient,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid source,
        string format = DefaultFormat,
        bool ephemeral = false,
        bool recordReplay = true,
        SoundSpecifier? sound = null,
        Color? color = null,
        string? name = null,
        string? font = null,
        int? fontSize = null);

    bool TryGetChannelFromMessage(
        string content,
        [NotNullWhen(true)] out ESChatChannelPrototype? channel,
        [NotNullWhen(true)] out string? trimmedContent);
}
