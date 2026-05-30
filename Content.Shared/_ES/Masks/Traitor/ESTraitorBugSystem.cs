using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Sparks;
using Content.Shared.Access;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Masks.Traitor;

public sealed partial class ESTraitorBugSystem : ESBaseObjectiveSystem<ESTraitorBugObjectiveComponent>
{
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESSparksSystem _sparks = default!;

    // TODO: This is mostly just a bad hack for the fact that you can't have a nullable value that's easily editable in VV.
    // I would like to not go insane editing all the APCs for this until we have an actual system that does it semi-reasonably.
    private static readonly ProtoId<AccessGroupPrototype> IgnoreDepartment = "AllAccess";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTraitorBuggableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ESTraitorBuggableComponent, ESPlantTraitorBugDoAfterEvent>(OnPlantTraitorBugDoAfter);
        SubscribeLocalEvent<ESTraitorBuggableComponent, ESTraitorBugTimerEvent>(OnTraitorBugTimer);
    }

    private void OnGetVerbs(Entity<ESTraitorBuggableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (ent.Comp.IsBugged)
        {
            // TODO: remove bug verb
        }

        if (CanBug(ent.Owner, args.User))
        {
            var user = args.User;
            args.Verbs.Add(new AlternativeVerb
            {
                Priority = 1,
                Text = Loc.GetString("es-bug-verb-text"),
                DoContactInteraction = true,
                Disabled = ent.Comp.IsBugged,
                Act = () =>
                {
                    if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                            user,
                            ent.Comp.BugPlantTime,
                            new ESPlantTraitorBugDoAfterEvent(),
                            eventTarget: ent,
                            ent)
                        {
                            BlockDuplicate = true,
                            DuplicateCondition = DuplicateConditions.SameEvent,
                            BreakOnMove = true,
                            BreakOnDamage = true,
                        }))
                        return;

                    _popup.PopupEntity(Loc.GetString("es-bug-popup-starting"), ent, PopupType.SmallCaution);
                },
            });
        }
    }

    private void OnPlantTraitorBugDoAfter(Entity<ESTraitorBuggableComponent> ent, ref ESPlantTraitorBugDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _popup.PopupEntity(Loc.GetString("es-bug-popup-planted"), ent, args.User);
        _sparks.DoSparks(ent, user: args.User, cooldown: false);

        ent.Comp.Timer = _entityTimer.SpawnTimer(ent, ent.Comp.BugDuration, new ESTraitorBugTimerEvent());
        Dirty(ent);

        args.Handled = true;
    }

    private void OnTraitorBugTimer(Entity<ESTraitorBuggableComponent> ent, ref ESTraitorBugTimerEvent args)
    {
        ent.Comp.Timer = null;
        Dirty(ent);

        // TODO: greytide virus goes here.

        // Globally increment all matching bug objectives. Maybe this should be user, specific, but it doesn't matter right now.
        foreach (var objective in ObjectivesSys.GetObjectives<ESTraitorBugObjectiveComponent>())
        {
            if (objective.Comp1.Target == ent.Comp.Department)
                ObjectivesSys.AdjustObjectiveCounter(objective.Owner);
        }
    }

    protected override void InitializeObjective(Entity<ESTraitorBugObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        var options = new HashSet<ProtoId<AccessGroupPrototype>>();

        var query = EntityQueryEnumerator<ESTraitorBuggableComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Department != IgnoreDepartment)
                options.Add(comp.Department);
        }

        if (options.Count == 0)
            return;

        var accessGroup = _prototype.Index(_random.Pick(options));

        ent.Comp.Target = accessGroup;
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("department", accessGroup.GetAccessGroupName())));
        Dirty(ent);
    }

    public bool CanBug(Entity<ESTraitorBuggableComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (ent.Comp.Department == IgnoreDepartment)
            return false;

        if (_admin.HasAdminFlag(user, AdminFlags.Debug))
            return true;

        if (!_mind.TryGetMind(user, out var mind))
            return false;

        foreach (var objective in ObjectivesSys.GetObjectives<ESTraitorBugObjectiveComponent>(mind.Value.Owner))
        {
            if (objective.Comp.Target == ent.Comp.Department)
                return true;
        }

        return false;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        if (!_net.IsServer)
            return;

        var query = EntityQueryEnumerator<ESTraitorBuggableComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            if (!comp.IsBugged)
                continue;

            if (_random.Prob(comp.BuggedSparkChance * frameTime))
                _sparks.DoSparks(uid);
        }
    }
}
