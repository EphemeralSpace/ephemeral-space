using Content.Shared._ES.Degradation;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.Objectives;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using Content.Shared.Whitelist;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Traitor;

public sealed class ESSabotageSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly ESDegradationSystem _degradation = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSabotageTargetComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ESSabotageTargetComponent, ESSabotageDoAfterEvent>(OnSabotage);
        SubscribeLocalEvent<ESSabotageTargetComponent, ExaminedEvent>(OnExamined);
    }

    /// <summary>
    ///     Returns true if the user should be capable of sabotaging the given target.
    /// </summary>
    [PublicAPI]
    public bool CanSabotage(EntityUid user, Entity<ESSabotageTargetComponent> target)
    {
        // for localhost debugging
        if (_admin.HasAdminFlag(user, AdminFlags.Debug))
            return true;

        if (_mind.GetMind(user) is not { } mind)
            return false;

        // overriding, for vandal etc
        if (HasComp<ESCanAlwaysSabotageComponent>(user) || HasComp<ESCanAlwaysSabotageComponent>(mind))
            return true;

        foreach (var objective in _objective.GetObjectives<ESSabotageConditionComponent>(mind))
        {
            if (_entityWhitelist.IsWhitelistPass(objective.Comp.Whitelist, target))
                return true;
        }

        return false;
    }

    private void OnGetVerbs(Entity<ESSabotageTargetComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!CanSabotage(args.User, ent))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 1,
            Text = Loc.GetString("es-sabotage-verb-text"),
            DoContactInteraction = true,
            Act = () =>
            {
                if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                        user,
                        ent.Comp.SabotageTime,
                        new ESSabotageDoAfterEvent(),
                        eventTarget: ent,
                        ent)
                    {
                        BlockDuplicate = true,
                        DuplicateCondition = DuplicateConditions.SameEvent,
                        BreakOnMove = true,
                        BreakOnDamage = true,
                    }))
                    return;

                _popup.PopupPredicted(Loc.GetString("es-sabotage-popup-starting"), ent, user, PopupType.SmallCaution);
            },
        });
    }

    private void OnSabotage(Entity<ESSabotageTargetComponent> ent, ref ESSabotageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanSabotage(args.User, ent))
            return;

        _degradation.Degrade(ent, args.User);

        var ev = new ESSabotageCompletedEvent(args.User, ent);
        RaiseLocalEvent(ref ev);

        args.Handled = true;
    }

    private void OnExamined(Entity<ESSabotageTargetComponent> ent, ref ExaminedEvent args)
    {
        if (!CanSabotage(args.Examiner, ent))
            return;

        args.PushMarkup(Loc.GetString("es-sabotage-examine-text"));
    }
}

[Serializable, NetSerializable]
public sealed partial class ESSabotageDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}

/// <summary>
/// Event broadcast whenever a sabotage is completed successfully.
/// </summary>
[ByRefEvent]
public readonly record struct ESSabotageCompletedEvent(EntityUid User, EntityUid Target);
