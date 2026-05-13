using Content.Shared._ES.Chat;

namespace Content.Client._ES.Chat;

public interface IESChatManager : IESSharedChatManager
{
    event Action<ESChatMessage>? OnChatMessageSent;
}

