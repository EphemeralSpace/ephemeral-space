using Content.Shared._ES.Chat;
using Content.Shared._ES.Chat.Components;

namespace Content.Server._ES.Chat;

/// <inheritdoc/>
public sealed class ESChatSystem : ESSharedChatSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESClientRefreshChatPermissions>(OnClientRefreshChatPermissions);
    }

    private void OnClientRefreshChatPermissions(ESClientRefreshChatPermissions msg, EntitySessionEventArgs args)
    {
        if (args.SenderSession.AttachedEntity is { } attachedEntity)
            RefreshChatPermissions(attachedEntity);
    }
}
