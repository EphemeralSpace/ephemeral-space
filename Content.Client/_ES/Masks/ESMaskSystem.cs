using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.Mind.Components;
using Content.Shared.StatusIcon.Components;
using Robust.Client.GameObjects;
using Robust.Client.Player;
using Robust.Shared.Prototypes;

namespace Content.Client._ES.Masks;

public sealed class ESMaskSystem : ESSharedMaskSystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly ContainerSystem _container = default!;

    public event Action<EntityUid, ProtoId<ESMaskPrototype>?>? OnMaskChanged;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMaskRoleComponent, AfterAutoHandleStateEvent>(OnRoleAfterHandleState);

        SubscribeLocalEvent<MindContainerComponent, GetStatusIconsEvent>(OnGetStagehandStatusIcons);
        SubscribeLocalEvent<ESTroupeFactionIconComponent, GetStatusIconsEvent>(OnGetStatusIcons);
    }

    private void OnRoleAfterHandleState(Entity<ESMaskRoleComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_container.TryGetContainingContainer(ent.Owner, out var roleContainer))
            return;
        var mind = roleContainer.Owner;
        OnMaskChanged?.Invoke(mind, ent.Comp.Mask);
    }

    private void OnGetStagehandStatusIcons(Entity<MindContainerComponent> ent, ref GetStatusIconsEvent args)
    {
        // Only stagehands should see the meta troupe icons.
        // Normal players will never receive the data anyways, but it prevents useless info
        // from bloating up the screen since they have no need for them.
        if (!HasComp<ESStagehandComponent>(_player.LocalEntity))
            return;

        if (!TryGetTroupe(ent, out var troupe))
            return;

        args.StatusIcons.Add(PrototypeManager.Index(PrototypeManager.Index(troupe.Value).MetaIcon));
    }

    private void OnGetStatusIcons(Entity<ESTroupeFactionIconComponent> ent, ref GetStatusIconsEvent args)
    {
        if (_player.LocalEntity is not { } local)
            return;

        // The main filtering is done on the networking for ESTroupeFactionIconComponent,
        // but this exists largely to catch edge cases where we still have
        // the networked comp on the client even though we shouldn't have access to it.
        if (GetTroupeOrNull(local) != ent.Comp.Troupe)
            return;
        args.StatusIcons.Add(PrototypeManager.Index(ent.Comp.Icon));
    }
}
