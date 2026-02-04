using System.Linq;
using Content.Shared._ES.Telesci.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared._ES.Telesci.Anomaly;

public sealed class ESSharedAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPortalAnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESPortalAnomalyComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ESAnomalyProbeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESAnomalyProbeComponent, AfterInteractEvent>(OnProbeAfterInteract);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ESProbeAnomalyDoAfterEvent>(OnProbeAnomalyDoAfter);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ItemToggleActivateAttemptEvent>(OnToggleActivateAttempt);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ItemToggleDeactivateAttemptEvent>(OnToggleDeactivateAttempt);
    }

    private void OnExamined(Entity<ESAnomalyProbeComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-anomaly-probe-mode-examine", ("mode", IsProbeMode(ent.AsNullable()))));
    }

    private void OnMapInit(Entity<ESPortalAnomalyComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.SignalCode = _random.GetItems(
            Enum.GetValues<ESAnomalySignal>(),
            ent.Comp.CodeLength,
            allowDuplicates: true)
            .ToList();
        Dirty(ent);
    }

    private void OnShutdown(Entity<ESPortalAnomalyComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<ESAnomalyConsoleComponent>();
        while (query.MoveNext(out var comp))
        {
            comp.Anomalies.Remove(ent);
        }
    }

    private void OnProbeAfterInteract(Entity<ESAnomalyProbeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target ||
            !HasComp<ESPortalAnomalyComponent>(target))
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (IsResonateMode(ent.AsNullable()))
        {

            _useDelay.TryResetDelay(ent.Owner);
        }
        else if (IsProbeMode(ent.AsNullable()))
        {
            _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.ProbeTime,
                new ESProbeAnomalyDoAfterEvent(),
                ent,
                target,
                ent)
            {
                DuplicateCondition = DuplicateConditions.None,
                BreakOnMove = false,
                NeedHand = true,
            });

            ent.Comp.InUse = true;
            Dirty(ent);
        }

        args.Handled = true;
    }

    private void OnProbeAnomalyDoAfter(Entity<ESAnomalyProbeComponent> ent, ref ESProbeAnomalyDoAfterEvent args)
    {
        ent.Comp.InUse = false;
        Dirty(ent);
        if (args.Cancelled || args.Handled)
            return;

        if (args.Target is not { } target ||
            !HasComp<ESPortalAnomalyComponent>(target))
            return;

        _audio.PlayPredicted(ent.Comp.CompleteSound, ent, args.User);
        _popup.PopupPredicted(Loc.GetString("es-anomaly-probe-completed-probe"), target, args.User, PopupType.Medium);
        var query = EntityQueryEnumerator<ESAnomalyConsoleComponent>();
        while (query.MoveNext(out var comp))
        {
            comp.Anomalies.Add(target);
        }

        args.Handled = true;
    }

    private void OnToggleActivateAttempt(Entity<ESAnomalyProbeComponent> ent, ref ItemToggleActivateAttemptEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = ent.Comp.InUse;
    }

    private void OnToggleDeactivateAttempt(Entity<ESAnomalyProbeComponent> ent, ref ItemToggleDeactivateAttemptEvent args)
    {
        if (!args.Cancelled)
            args.Cancelled = ent.Comp.InUse;
    }

    public bool IsResonateMode(Entity<ESAnomalyProbeComponent?> ent)
    {
        return !IsProbeMode(ent);
    }

    public bool IsProbeMode(Entity<ESAnomalyProbeComponent?> ent)
    {
        return _itemToggle.IsActivated(ent.Owner);
    }
}
