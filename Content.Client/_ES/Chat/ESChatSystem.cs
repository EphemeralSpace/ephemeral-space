using Content.Shared._ES.Chat;
using Content.Shared._ES.Chat.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Chat;

/// <inheritdoc/>
public sealed partial class ESChatSystem : ESSharedChatSystem
{
    [Dependency] private INetManager _net = default!;

    public event Action<EntityUid, HashSet<ProtoId<ESChatChannelPrototype>>>? LocalChatPermissionsUpdated;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChatPermissionsComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESChatPermissionsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESChatPermissionsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);
    }

    public override void RefreshChatPermissions(Entity<ESChatPermissionsComponent?> ent)
    {
        base.RefreshChatPermissions(ent);

        if (_net.IsConnected)
            RaiseNetworkEvent(new ESClientRefreshChatPermissions());
    }

    private void OnAfterAutoHandleState(Entity<ESChatPermissionsComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (PlayerManager.LocalEntity != ent)
            return;

        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }

    private void OnPlayerAttached(Entity<ESChatPermissionsComponent> ent, ref LocalPlayerAttachedEvent args)
    {
        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }

    private void OnPlayerDetached(Entity<ESChatPermissionsComponent> ent, ref LocalPlayerDetachedEvent args)
    {
        LocalChatPermissionsUpdated?.Invoke(ent, ent.Comp.PermittedChannels);
    }
}
