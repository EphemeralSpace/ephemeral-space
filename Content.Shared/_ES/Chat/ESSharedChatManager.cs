using System.Diagnostics.CodeAnalysis;
using Robust.Shared.Audio;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chat;

public abstract partial class ESSharedChatManager : IESSharedChatManager
{
    [Dependency] protected IPrototypeManager PrototypeManager = default!;

    public event Action<EntityUid, ESRequestSendChatMessage>? OnRequestSendChatMessage;

    public virtual void Initialize()
    {

    }

    protected void InvokeRequestSendChatMessage(EntityUid uid, ESRequestSendChatMessage msg)
    {
        OnRequestSendChatMessage?.Invoke(uid, msg);
    }

    public abstract void SendChatMessage(string content,
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
