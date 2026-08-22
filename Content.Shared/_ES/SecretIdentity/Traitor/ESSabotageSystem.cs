using Content.Shared._ES.Degradation;
using Content.Shared._ES.SecretIdentity.Traitor.Components;
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

namespace Content.Shared._ES.SecretIdentity.Traitor;

public sealed partial class ESSabotageSystem : EntitySystem
{
    [Dependency] private ISharedAdminManager _admin = default!;
    [Dependency] private ESDegradationSystem _degradation = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private ESSharedObjectiveSystem _objective = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedMindSystem _mind = default!;

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
    public bool CanSabotage(EntityUid user, Entity<ESSabotageTargetComponent?> target)
    {
        if (!Resolve(target, ref target.Comp))
            return false;

        // for localhost debugging
        if (_admin.HasAdminFlag(user, AdminFlags.Debug))
            return true;

        if (_mind.GetMind(user) is not { } mind)
            return false;

        var ev = new ESSabotageAttemptEvent(user);
        RaiseLocalEvent(target, ref ev);
        if (ev.Cancelled)
            return false;

        // overriding, for vandal etc
        if (HasComp<ESCanAlwaysSabotageComponent>(user) || HasComp<ESCanAlwaysSabotageComponent>(mind))
            return true;

        foreach (var objective in _objective.GetObjectives<ESSabotageConditionComponent>(mind))
        {
            if (!_entityWhitelist.IsWhitelistPass(objective.Comp.Whitelist, target))
                continue;

            if (_objective.IsCompleted(objective.Owner))
                continue;

            return true;
        }

        return false;
    }

    private void OnGetVerbs(Entity<ESSabotageTargetComponent> ent, ref GetVerbsEvent<AlternativeVerb> args)
    {
        if (!CanSabotage(args.User, ent.AsNullable()))
            return;

        var user = args.User;
        args.Verbs.Add(new AlternativeVerb
        {
            Priority = 1,
            Text = Loc.GetString("es-sabotage-verb-text"),
            Disabled = !args.CanAccess || !args.CanInteract,
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

                _popup.PopupEntity(Loc.GetString("es-sabotage-popup-starting"), ent, PopupType.SmallCaution);
            },
        });
    }

    private void OnSabotage(Entity<ESSabotageTargetComponent> ent, ref ESSabotageDoAfterEvent args)
    {
        if (args.Cancelled || args.Handled)
            return;

        if (!CanSabotage(args.User, ent.AsNullable()))
            return;

        _degradation.Degrade(ent, args.User);

        var ev = new ESSabotageCompletedEvent(args.User, ent);
        RaiseLocalEvent(ref ev);

        args.Handled = true;
    }

    private void OnExamined(Entity<ESSabotageTargetComponent> ent, ref ExaminedEvent args)
    {
        if (!CanSabotage(args.Examiner, ent.AsNullable()))
            return;

        args.PushMarkup(Loc.GetString("es-sabotage-examine-text"));
    }
}

[Serializable, NetSerializable]
public sealed partial class ESSabotageDoAfterEvent : SimpleDoAfterEvent;

/// <summary>
/// Event broadcast whenever a sabotage is completed successfully.
/// </summary>
[ByRefEvent]
public readonly record struct ESSabotageCompletedEvent(EntityUid User, EntityUid Target);

/// <summary>
/// Event raised on a sabotage target to check whether it can currently be sabotaged.
/// </summary>
[ByRefEvent]
public record struct ESSabotageAttemptEvent(EntityUid User, bool Cancelled = false);
