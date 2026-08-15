using Content.Server._ES.Chat.Admin.Components;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared._ES.Chat;
using Robust.Shared.Player;

namespace Content.Server._ES.Chat.Admin;

public sealed partial class ESAdminChatChannelSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private ESSharedChatSystem _chat = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESGetChatPermissionsEvent>(OnGetChatPermissions);
        SubscribeLocalEvent<PlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<PlayerDetachedEvent>(OnPlayerDetached);
        SubscribeLocalEvent<ESAdminChatChannelComponent, ESGetChatMessageRecipientsEvent>(OnGetRecipients);

        _admin.OnPermsChanged += OnPermsChanged;
    }

    private void OnGetChatPermissions(ref ESGetChatPermissionsEvent args)
    {
        if (!_admin.IsAdmin(args.Source))
            return;

        foreach (var ent in EntityQueryEnumerator<ESAdminChatChannelComponent>())
        {
            if (!ent.Comp.AdminSendable)
                continue;
            args.Channels.Add(_chat.GetChannel(ent.Owner));
        }
    }

    private void OnPlayerAttached(PlayerAttachedEvent ev)
    {
        if (_admin.IsAdmin(ev.Player))
            _chat.RefreshChatPermissions(ev.Entity);
    }

    private void OnPlayerDetached(PlayerDetachedEvent ev)
    {
        if (_admin.IsAdmin(ev.Player))
            _chat.RefreshChatPermissions(ev.Entity);
    }

    private void OnGetRecipients(Entity<ESAdminChatChannelComponent> ent, ref ESGetChatMessageRecipientsEvent args)
    {
        foreach (var admin in _admin.ActiveAdmins)
        {
            if (admin.AttachedEntity is { } attachedEntity)
                args.AddRecipient(attachedEntity);
        }
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs args)
    {
        if (args.Player.AttachedEntity is { } attached)
            _chat.RefreshChatPermissions(attached);
    }
}
