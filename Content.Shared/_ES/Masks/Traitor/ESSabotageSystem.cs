using Content.Shared._ES.Degradation;
using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared.Administration;
using Content.Shared.Administration.Managers;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Content.Shared.Popups;
using Content.Shared.Verbs;
using JetBrains.Annotations;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Traitor;

public sealed class ESSabotageSystem : EntitySystem
{
    [Dependency] private readonly ISharedAdminManager _admin = default!;
    [Dependency] private readonly ESDegradationSystem _degradation = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ESSharedMaskSystem _mask = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSabotageTargetComponent, GetVerbsEvent<AlternativeVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ESSabotageTargetComponent, ESSabotageDoAfterEvent>(OnSabotage);
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

        // overriding, for vandal etc
        if (HasComp<ESCanAlwaysSabotageComponent>(user)
            || (_mind.GetMind(user) is { } mind && HasComp<ESCanAlwaysSabotageComponent>(mind)))
            return true;

        return _mask.GetTroupeOrNull(user) == target.Comp.SabotageTroupe;
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
