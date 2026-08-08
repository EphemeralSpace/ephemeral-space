using Content.Shared._ES.Chat;
using Content.Shared._ES.Chat.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Chat;

/// <inheritdoc/>
public sealed class ESChatSystem : ESSharedChatSystem
{
    public event Action<EntityUid, HashSet<ProtoId<ESChatChannelPrototype>>>? LocalChatPermissionsUpdated;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChatPermissionsComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESChatPermissionsComponent, PlayerAttachedEvent>(OnPlayerAttached);
    }

    private void OnAfterAutoHandleState(Entity<ESChatPermissionsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }

    private void OnPlayerAttached(Entity<ESChatPermissionsComponent> ent, ref PlayerAttachedEvent args)
    {
        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }

    public override void RefreshChatPermissions(Entity<ESChatPermissionsComponent?> ent)
    {
        base.RefreshChatPermissions(ent);

        if (!Resolve(ent, ref ent.Comp, false))
            return;

        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }
}
