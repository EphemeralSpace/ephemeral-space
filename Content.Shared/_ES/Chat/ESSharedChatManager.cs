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

    public bool TryGetChannelFromMessage(string content, [NotNullWhen(true)] out ESChatChannelPrototype? channel)
    {
        channel = null;

        content = content.Trim();
        if (content.Length == 0)
            return false;

        var c = content[0];
        foreach (var channelPrototype in PrototypeManager.EnumeratePrototypes<ESChatChannelPrototype>())
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
