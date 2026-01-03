using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Body.Systems;
using Content.Shared._ES.Objectives;
using Content.Shared.Body.Components;
using Content.Shared.IdentityManagement;
using Content.Shared.Mind.Components;
using Content.Shared.Mobs;
using Content.Shared.Popups;
using Robust.Shared.Player;

namespace Content.Server._ES.Masks.Objectives;

/// <summary>
///     Handles gameplay behavior and objective logic for the Sacrifice's healing-on-death.
/// </summary>
/// <seealso cref="ESSacrificeHealObjectiveComponent"/>
public sealed class ESSacrificeHealObjectiveSystem : ESBaseObjectiveSystem<ESSacrificeHealObjectiveComponent>
{
    [Dependency] private readonly BloodstreamSystem _bloodstream = default!;
    [Dependency] private readonly EntityLookupSystem _lookup = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;

    public override Type[] RelayComponents => [typeof(ESMobStateRelayComponent)];

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSacrificeHealObjectiveComponent, ESMobStateChanged>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESSacrificeHealObjectiveComponent> ent, ref ESMobStateChanged args)
    {
        if (args.NewMobState != MobState.Dead)
            return;

        var coords = Transform(args.Mob.Owner).Coordinates;

        var count = 0;
        var msg = Loc.GetString(ent.Comp.SacrificePopupMessage, ("name", Identity.Name(args.Mob.Owner, EntityManager)));

        foreach (var otherMob in _lookup.GetEntitiesInRange<MindContainerComponent>(coords, ent.Comp.Radius, LookupFlags.Uncontained))
        {
            if (otherMob.Owner == args.Mob.Owner)
                continue;

            // only inject mobs that 1) can have a mind, 2) do have a mind, and 3) have a bloodstream
            if (!TryComp<BloodstreamComponent>(otherMob, out var bloodstream)) //|| !otherMob.Comp.HasMind)
                continue;

            // add chems, increment count, and show popup
            if (!_bloodstream.TryAddToBloodstream((otherMob.Owner, bloodstream), ent.Comp.SacrificeSolution.Clone()))
                continue;

            count++;

            if (!TryComp<ActorComponent>(otherMob, out var actor))
                continue;

            _popup.PopupEntity(msg, otherMob.Owner, actor.PlayerSession, PopupType.Large);
        }

        ObjectivesSys.SetObjectiveCounter(ent.Owner, count);
    }
}
