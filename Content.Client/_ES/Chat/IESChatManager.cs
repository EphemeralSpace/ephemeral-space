using Content.Shared._ES.Chat;
using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Chat;

public interface IESChatManager : IESSharedChatManager
{
    event Action<ESChatMessage>? OnChatMessageSent;

    void RequestSendChatMessage(string message,
        ProtoId<ESChatChannelPrototype> channel,
        ProtoId<RadioChannelPrototype>? radio);
}

