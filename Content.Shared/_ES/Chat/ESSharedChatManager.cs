using System.Diagnostics.CodeAnalysis;
using Content.Shared.CCVar;
using Robust.Shared.Audio;
using Robust.Shared.Configuration;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Chat;

public abstract partial class ESSharedChatManager : IESSharedChatManager
{
    [Dependency] private IConfigurationManager _configuration = default!;
    [Dependency] protected IPrototypeManager PrototypeManager = default!;

    public event Action<EntityUid, string, ProtoId<ESChatChannelPrototype>>? OnRequestSendChatMessage;

    public int MaxMessageLength { get; private set; }

    public virtual void Initialize()
    {
        _configuration.OnValueChanged(CCVars.ChatMaxMessageLength, v => { MaxMessageLength = v; }, true);
    }

    public void SendServerMessage(string content, Color? color = null)
    {
        SendServerMessage(content, Filter.GetAllPlayers(), color);
    }

    public void SendServerMessage(string content, ICommonSession session, Color? color = null)
    {
        SendServerMessage(content, [session], color);
    }

    public void SendServerMessage(string content, IEnumerable<ICommonSession> session, Color? color = null)
    {
        var wrappedMessage = Loc.GetString("chat-manager-server-wrap-message", ("message", FormattedMessage.EscapeText(content)));
        SendChatMessage(wrappedMessage, session, IESSharedChatManager.ServerChannel, EntityUid.Invalid, color: color);
    }

    protected void InvokeRequestSendChatMessage(EntityUid uid, string content, ProtoId<ESChatChannelPrototype> channel)
    {
        OnRequestSendChatMessage?.Invoke(uid, content, channel);
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
        SendChatMessage(
            content,
            [recipient],
            channel,
            source,
            format,
            ephemeral,
            recordReplay,
            sound,
            color,
            name,
            font,
            fontSize);
    }

    public abstract void SendChatMessage(string content,
        IEnumerable<ICommonSession> recipient,
        ProtoId<ESChatChannelPrototype> channel,
        EntityUid source,
        string format = IESSharedChatManager.DefaultFormat,
        bool ephemeral = false,
        bool recordReplay = true,
        SoundSpecifier? sound = null,
        Color? color = null,
        string? name = null,
        string? font = null,
        int? fontSize = null);

    public bool TryGetChannelFromMessage(string content,
        [NotNullWhen(true)] out ESChatChannelPrototype? channel,
        [NotNullWhen(true)] out string? trimmedContent)
    {
        channel = null;
        trimmedContent = null;

        content = content.Trim();
        if (content.Length == 0)
            return false;

        foreach (var channelPrototype in PrototypeManager.EnumeratePrototypes<ESChatChannelPrototype>())
        {
            foreach (var prefix in channelPrototype.Prefixes)
            {
                if (content.StartsWith(prefix))
                {
                    trimmedContent = content.Substring(prefix.Length);
                    channel = channelPrototype;
                    return true;
                }
            }
        }

        return false;

    }
}
