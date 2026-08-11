using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public interface IESSharedChatManager
{
    const string DefaultFormat = "{0}";
    static readonly ProtoId<ESChatChannelPrototype> ServerChannel = "Server";

    event Action<EntityUid, string, ProtoId<ESChatChannelPrototype>>? OnRequestSendChatMessage;

    void Initialize();

    void SendServerMessage(string content, Color? color = null);

    void SendServerMessage(string content, ICommonSession session, Color? color = null);

    void SendServerMessage(string content, IEnumerable<ICommonSession> session, Color? color = null);

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

    void SendChatMessage(
        string content,
        IEnumerable<ICommonSession> recipients,
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
