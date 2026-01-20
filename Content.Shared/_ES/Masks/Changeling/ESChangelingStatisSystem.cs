using Content.Shared._ES.Mind;
using Content.Shared.Actions;
using Content.Shared.Actions.Components;
using Content.Shared.Damage.Systems;
using Content.Shared.IdentityManagement;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;
using Content.Shared.Roles.Components;

namespace Content.Shared._ES.Changeling;

public sealed class ESChangelingStatisSystem : EntitySystem
{

    [Dependency] private readonly SharedActionsSystem _actions = default!;
    [Dependency] private readonly DamageableSystem _damage = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ChangelingRoleComponent, MobStateChangedEvent>(OnMobStateChanged);
        SubscribeLocalEvent<ChangelingRoleComponent, ESGhostAttemptEvent>(OnTryGhost);
        SubscribeLocalEvent<ESChangelingStatisEvent>(OnChangelingStatisEvent);
    }

    private void OnMobStateChanged(Entity<ChangelingRoleComponent> ent, ref MobStateChangedEvent args)
    {
        if (_mobState.IsAlive(ent))
        {
            if (ent.Comp.StatisActionEntity == null)
                return;

            if (!TryComp<ActionsComponent>(ent, out var action))
                return;

            _actions.RemoveAction((ent, action), ent.Comp.StatisActionEntity);
        }
    }

    private void OnChangelingStatisEvent(ESChangelingStatisEvent args)
    {
        if (_mobState.IsCritical(args.Performer))
        {
            _mobState.ChangeMobState(args.Performer, MobState.Dead);
            return;
        }

        _damage.ClearAllDamage(args.Performer);
        _mobState.ChangeMobState(args.Performer, MobState.Alive);

        var selfMessage = Loc.GetString("changeling-statis-end-self", ("user", Identity.Entity(args.Performer, EntityManager)));
        var othersMessage = Loc.GetString("changeling-statis-end-others", ("user", Identity.Entity(args.Performer, EntityManager)));

        _popup.PopupPredicted(selfMessage, othersMessage, args.Performer, args.Performer, type: PopupType.MediumCaution);
    }

    private void OnTryGhost(Entity<ChangelingRoleComponent> ent, ref ESGhostAttemptEvent args)
    {
        if (_mobState.IsIncapacitated(ent))
        {
            args.Cancelled = true;
        }
    }
}
