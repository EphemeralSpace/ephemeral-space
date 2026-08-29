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
    public event Action<ProtoId<ESChatChannelPrototype>>? ChatChannelFocused;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChatPermissionsComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESChatPermissionsComponent, LocalPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESChatPermissionsComponent, LocalPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ESChatPermissionsComponent, ESChatFocusChannelActionEvent>(OnChatFocusChannelAction);
    }

    public override void RefreshChatPermissions(Entity<ESChatPermissionsComponent?> ent)
    {
        base.RefreshChatPermissions(ent);

        // Due to certain events (containers) which are raised on the client but not the server,
        // it's possible for chat permissions to refresh only on the client and result in a state
        // that is desynced from the server. To compensate, we just always ask the server to refresh
        // the values when we update the client. This sometimes duplicates the state handling but
        // if there are no changes it shouldn't have an effect.
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

    private void OnChatFocusChannelAction(Entity<ESChatPermissionsComponent> ent, ref ESChatFocusChannelActionEvent args)
    {
        FocusChatChannel(args.Channel);
        args.Handled = true;
    }

    public void FocusChatChannel(ProtoId<ESChatChannelPrototype> channel)
    {
        ChatChannelFocused?.Invoke(channel);
    }
}
