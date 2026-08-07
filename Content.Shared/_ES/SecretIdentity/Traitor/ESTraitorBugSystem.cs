using Content.Shared._ES.Breakable;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.SecretIdentity.Traitor.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Sparks;
using Content.Shared._ES.Stagehand;
using Content.Shared.Access;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Robust.Shared.Network;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.SecretIdentity.Traitor;

public sealed partial class ESTraitorBugSystem : ESBaseObjectiveSystem<ESTraitorBugObjectiveComponent>
{
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private INetManager _net = default!;
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private ESBreakableSystem _breakable = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedMindSystem _mind = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private ESSparksSystem _sparks = default!;
    [Dependency] private ESSharedStagehandNotificationsSystem _notification = default!;

    // TODO: This is mostly just a bad hack for the fact that you can't have a nullable value that's easily editable in VV.
    // I would like to not go insane editing all the APCs for this until we have an actual system that does it semi-reasonably.
    private static readonly ProtoId<AccessGroupPrototype> IgnoreDepartment = "AllAccess";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTraitorBuggableComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESTraitorBuggableComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ESTraitorBuggableComponent, ESPlantTraitorBugDoAfterEvent>(OnPlantTraitorBugDoAfter);
        SubscribeLocalEvent<ESTraitorBuggableComponent, ESRemoveTraitorBugDoAfterEvent>(OnRemoveTraitorBugDoAfter);
        SubscribeLocalEvent<ESTraitorBuggableComponent, ESTraitorBugTimerEvent>(OnTraitorBugTimer);
        SubscribeLocalEvent<ESTraitorBuggableComponent, ESBrokenStateChanged>(OnBrokenStateChanged);
    }

    private void OnExamined(Entity<ESTraitorBuggableComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ESTraitorBuggableComponent)))
        {
            if (ent.Comp.IsBugged && args.IsInDetailsRange)
            {
                var progress = _entityTimer.GetTimerProgress(ent.Comp.Timer.Value);
                args.PushMarkup(Loc.GetString("es-bugging-progress-examine-text", ("progress", (int) (progress * 100))));
            }

            if (CanBug(ent.AsNullable(), args.Examiner))
            {
                args.PushMarkup(Loc.GetString("es-bugging-examine-text"));
            }
        }
    }

    private void OnGetVerbs(Entity<ESTraitorBuggableComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!args.CanAccess)
            return;

        if (ent.Comp.IsBugged)
        {
            var user = args.User;
            args.Verbs.Add(new AlternativeVerb
            {
                Priority = 2,
                Text = Loc.GetString("es-remove-bug-verb-text"),
                DoContactInteraction = true,
                Act = () =>
                {
                    if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                            user,
                            ent.Comp.BugRemoveTime,
                            new ESRemoveTraitorBugDoAfterEvent(),
                            eventTarget: ent,
                            ent)
                        {
                            DuplicateCondition = DuplicateConditions.SameEvent,
                            BreakOnMove = true,
                            BreakOnDamage = true,
                        }))
                        return;

                    _popup.PopupEntity(Loc.GetString("es-remove-bug-popup"), ent);
                },
            });
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
        if (args.Cancelled || !CanBug(ent.AsNullable(), args.User))
            return;

        _popup.PopupEntity(Loc.GetString("es-bug-popup-planted"), ent, args.User);
        _sparks.DoSparks(ent, user: args.User, cooldown: false);

        _appearance.SetData(ent, ESTraitorBugVisuals.Bugged, true);
        ent.Comp.Timer = _entityTimer.SpawnTimer(ent, ent.Comp.BugDuration, new ESTraitorBugTimerEvent());

        _notification.SendStagehandNotification(Loc.GetString("es-stagehand-notification-apc-bugged",
            ("buggable", _notification.WrapEntityName(ent.Owner)),
            ("player", _notification.WrapEntityName(args.User))));

        Dirty(ent);

        args.Handled = true;
    }

    private void OnRemoveTraitorBugDoAfter(Entity<ESTraitorBuggableComponent> ent, ref ESRemoveTraitorBugDoAfterEvent args)
    {
        if (args.Cancelled)
            return;

        _notification.SendStagehandNotification(Loc.GetString("es-stagehand-notification-apc-bug-removed",
            ("buggable", _notification.WrapEntityName(ent.Owner)),
            ("player", _notification.WrapEntityName(args.User))));

        CancelBug(ent.AsNullable());
        args.Handled = true;
    }

    private void OnTraitorBugTimer(Entity<ESTraitorBuggableComponent> ent, ref ESTraitorBugTimerEvent args)
    {
        CancelBug(ent.AsNullable());

        _sparks.DoSparks(ent);
        var ev = new ESTraitorBugHackedEvent(ent.Comp.Department);
        RaiseLocalEvent(ref ev);

        // Globally increment all matching bug objectives. Maybe this should be user, specific, but it doesn't matter right now.
        foreach (var objective in ObjectivesSys.GetObjectives<ESTraitorBugObjectiveComponent>())
        {
            if (objective.Comp1.Target == ent.Comp.Department)
                ObjectivesSys.AdjustObjectiveCounter(objective.Owner);
        }
    }

    private void OnBrokenStateChanged(Entity<ESTraitorBuggableComponent> ent, ref ESBrokenStateChanged args)
    {
        if (args.Broken)
            CancelBug(ent.AsNullable());
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

    public void CancelBug(Entity<ESTraitorBuggableComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        _appearance.SetData(ent, ESTraitorBugVisuals.Bugged, false);

        PredictedDel(ent.Comp.Timer);
        ent.Comp.Timer = null;
        Dirty(ent);
    }

    public bool CanBug(Entity<ESTraitorBuggableComponent?> ent, EntityUid user)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (_breakable.IsBroken(ent.Owner))
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

        // We don't bother predicting this. It wouldn't matter in any case.
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
