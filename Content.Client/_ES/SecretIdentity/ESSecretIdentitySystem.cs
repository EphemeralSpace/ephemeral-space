using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Mind.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.SecretIdentity;

public sealed partial class ESSecretIdentitySystem : ESSharedSecretIdentitySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private ContainerSystem _container = default!;

    public event Action<EntityUid, ProtoId<ESSecretIdentityPrototype>?>? OnSecretIdentityChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSecretIdentityRoleComponent, AfterAutoHandleStateEvent>(OnRoleAfterHandleState);

        SubscribeLocalEvent<MindContainerComponent, GetStatusIconsEvent>(OnGetStagehandStatusIcons);
        SubscribeLocalEvent<ESOrganizationFactionIconComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnRoleAfterHandleState(Entity<ESSecretIdentityRoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var roleContainer))
            return;
        var mind = roleContainer.Owner;
        OnSecretIdentityChanged?.Invoke(mind, ent.Comp.SecretIdentity);
    }

    private void OnGetStagehandStatusIcons(Entity<MindContainerComponent> ent, ref GetStatusIconsEvent args)
    {
        // Only stagehands should see the meta organization icons.
        // Normal players will never receive the data anyways, but it prevents useless info
        // from bloating up the screen since they have no need for them.
        if (!HasComp<ESStagehandComponent>(_player.LocalEntity))
            return;

        if (!TryGetOrganization(ent, out var organization))
            return;

        args.StatusIcons.Add(PrototypeManager.Index(PrototypeManager.Index(organization.Value).MetaIcon));
    }

    private void OnGetStatusIcons(Entity<ESOrganizationFactionIconComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity is not { } local)
            return;

        // The main filtering is done on the networking for ESOrganizationFactionIconComponent,
        // but this exists largely to catch edge cases where we still have
        // the networked comp on the client even though we shouldn't have access to it.
        if (GetOrganizationOrNull(local) != ent.Comp.Organization)
            return;
        args.StatusIcons.Add(PrototypeManager.Index(ent.Comp.Icon));
    }
}
