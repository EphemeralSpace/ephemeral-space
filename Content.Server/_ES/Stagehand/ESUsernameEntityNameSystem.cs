using Content.Server._ES.Stagehand.Components;
using Content.Server.Administration;
using Content.Server.Administration.Managers;
using Content.Shared._ES.Stagehand.Components;
using Robust.Shared.Player;

namespace Content.Server._ES.Stagehand;

public sealed partial class ESUsernameEntityNameSystem : EntitySystem
{
    [Dependency] private IAdminManager _admin = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESUsernameEntityNameComponent, PlayerAttachedEvent>(OnPlayerAttached);

        _admin.OnPermsChanged += OnPermsChanged;
    }

    private void OnPlayerAttached(Entity<ESUsernameEntityNameComponent> ent, ref PlayerAttachedEvent args)
    {
        _metaData.SetEntityName(ent, args.Player.Name);
        _appearance.SetData(ent, ESUsernameEntityVisuals.Admin, _admin.IsAdmin(args.Player));
    }

    private void OnPermsChanged(AdminPermsChangedEventArgs obj)
    {
        if (!HasComp<ESUsernameEntityNameComponent>(obj.Player.AttachedEntity))
            return;
        _appearance.SetData(obj.Player.AttachedEntity.Value, ESUsernameEntityVisuals.Admin, _admin.IsAdmin(obj.Player));
    }
}
