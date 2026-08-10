using Content.Shared._ES.Chat;
using Content.Shared._ES.Chat.Components;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Chat;

/// <inheritdoc/>
public sealed partial class ESChatSystem : ESSharedChatSystem
{
    public event Action<EntityUid, HashSet<ProtoId<ESChatChannelPrototype>>>? LocalChatPermissionsUpdated;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChatPermissionsComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESChatPermissionsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESChatPermissionsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    private void OnAfterAutoHandleState(Entity<ESChatPermissionsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }

    private void OnPlayerAttached(Entity<ESChatPermissionsComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }

    private void OnPlayerDetached(Entity<ESChatPermissionsComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }
}
