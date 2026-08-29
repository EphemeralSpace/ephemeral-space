using System.Diagnostics.CodeAnalysis;
using Content.Shared.Administration;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public interface IESSharedChatManager
{
    const string DefaultFormat = "{0}";
    static readonly ProtoId<ESChatChannelPrototype> ServerChannel = "Server";
    static readonly ProtoId<ESChatChannelPrototype> AdminChannel = "Admin";

    static readonly ProtoId<ESChatChannelOrderPrototype> ChannelOrder = "Default";

    int MaxMessageLength { get; protected set; }

    event Action<EntityUid, string, ProtoId<ESChatChannelPrototype>>? OnRequestSendChatMessage;
    event Action<ESDiscordChannel, string, string>? OnDiscordHook;

    void Initialize();

    void SendServerMessage(string content, Color? color = null);
    void SendServerMessage(string content, ICommonSession session, Color? color = null);
    void SendServerMessage(string content, IEnumerable<ICommonSession> session, Color? color = null);

    void SendAdminMessage(string content, AdminFlags? flagBlacklist = null, AdminFlags? flagWhitelist = null);
    void SendAdminMessage(string content, ICommonSession session);
    void SendAdminMessage(string content, IEnumerable<ICommonSession> sessions);

    void SendChatMessage(
        string content,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid? source = null,
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
        ICommonSession recipient,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid? source = null,
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
        EntityUid? source = null,
        string format = DefaultFormat,
        bool ephemeral = false,
        bool recordReplay = true,
        SoundSpecifier? sound = null,
        Color? color = null,
        string? name = null,
        string? font = null,
        int? fontSize = null);

    void RecordReplayChatMessage(
        string content,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid? source = null,
        string format = DefaultFormat,
        bool ephemeral = false,
        SoundSpecifier? sound = null,
        Color? color = null,
        string? name = null,
        string? font = null,
        int? fontSize = null);

    void SendDiscordHookMessage(
        ESDiscordChannel channel,
        string name,
        string message);

    bool TryGetChannelFromMessage(
        string content,
        [NotNullWhen(true)] out ESChatChannelPrototype? channel,
        [NotNullWhen(true)] out string? trimmedContent);

    void DeleteMessagesBy(NetUserId uid);
}
