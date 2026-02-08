using System.Linq;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Sparks;
using Content.Shared._ES.Telesci.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Item.ItemToggle;
using Content.Shared.Item.ItemToggle.Components;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;

namespace Content.Shared._ES.Telesci.Anomaly;

public abstract class ESSharedAnomalySystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly ItemToggleSystem _itemToggle = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ESSparksSystem _sparks = default!;
    [Dependency] private readonly ESTimedDespawnSystem _timedDespawn = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPortalAnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESPortalAnomalyComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ESAnomalyProbeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESAnomalyProbeComponent, GetVerbsEvent<Verb>>(OnGetVerb);
        SubscribeLocalEvent<ESAnomalyProbeComponent, AfterInteractEvent>(OnProbeAfterInteract);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ESProbeAnomalyDoAfterEvent>(OnProbeAnomalyDoAfter);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ItemToggleActivateAttemptEvent>(OnToggleActivateAttempt);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ItemToggleDeactivateAttemptEvent>(OnToggleDeactivateAttempt);

        SubscribeLocalEvent<ESAnomalyConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpen);
    }

    private void OnExamined(Entity<ESAnomalyProbeComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ESAnomalyProbeComponent)))
        {
            args.PushMarkup(Loc.GetString("es-anomaly-probe-mode-examine", ("mode", IsProbeMode(ent.AsNullable()))));

            if (IsResonateMode(ent.AsNullable()))
                args.PushMarkup(Loc.GetString("es-anomaly-probe-mode-examine-signal", ("freq", GetSignalString(Loc, ent.Comp.CurrentSignal))));
        }
    }

    private void OnGetVerb(Entity<ESAnomalyProbeComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
            return;

        if (!IsResonateMode(ent.AsNullable()))
            return;

        var user = args.User;

        foreach (var signal in Enum.GetValues<ESAnomalySignal>())
        {
            var v = new Verb
            {
                Priority = 1,
                Category = VerbCategory.SelectType,
                Text = Loc.GetString("es-anomaly-probe-verb-fmt", ("freq", GetSignalString(Loc, signal))),
                Disabled = signal == ent.Comp.CurrentSignal,
                DoContactInteraction = true,
                Act = () =>
                {
                    SetProbeSignal(ent, signal);
                    _sparks.DoSparks(ent.Owner, 1, user: user);
                    _popup.PopupPredicted(Loc.GetString("es-anomaly-probe-popup-freq-set", ("type", GetSignalString(Loc, signal))), ent, user);
                },
            };
            args.Verbs.Add(v);
        }
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
        UpdateConsolesUi();
    }

    private void OnProbeAfterInteract(Entity<ESAnomalyProbeComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target ||
            !TryComp<ESPortalAnomalyComponent>(target, out var anom))
            return;

        if (_useDelay.IsDelayed(target))
            return;

        if (IsResonateMode(ent.AsNullable()))
        {
            if (TryUseSignal(ent, (target, anom), args.User))
                _useDelay.TryResetDelay(target);
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

        _sparks.DoSparks(ent, user: args.User);
        _audio.PlayPredicted(ent.Comp.CompleteSound, ent, args.User);
        _popup.PopupPredicted(Loc.GetString("es-anomaly-probe-completed-probe"), target, args.User, PopupType.Medium);
        var query = EntityQueryEnumerator<ESAnomalyConsoleComponent>();
        while (query.MoveNext(out var comp))
        {
            comp.Anomalies.Add(target);
        }
        UpdateConsolesUi();

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

    private void OnBeforeOpen(Entity<ESAnomalyConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi((ent, ent));
    }

    public bool IsResonateMode(Entity<ESAnomalyProbeComponent?> ent)
    {
        return !IsProbeMode(ent);
    }

    public bool IsProbeMode(Entity<ESAnomalyProbeComponent?> ent)
    {
        return _itemToggle.IsActivated(ent.Owner);
    }

    public void SetProbeSignal(Entity<ESAnomalyProbeComponent> ent, ESAnomalySignal signal)
    {
        ent.Comp.CurrentSignal = signal;
        _appearance.SetData(ent, ESAnomalyProbeVisuals.Mode, signal);
        Dirty(ent);
    }

    public static string GetSignalString(ILocalizationManager loc, ESAnomalySignal signal)
    {
        return loc.GetString($"es-anomaly-signal-{signal}");
    }

    public bool TryUseSignal(Entity<ESAnomalyProbeComponent> probe, Entity<ESPortalAnomalyComponent> anom, EntityUid? user)
    {
        if (anom.Comp.CodeIndex == anom.Comp.CodeLength)
            return false;

        var targetSignal = anom.Comp.SignalCode[anom.Comp.CodeIndex];

        if (probe.Comp.CurrentSignal != targetSignal)
        {
            PulseAnomalyRadiation(anom, user);
            return false;
        }

        _audio.PlayPredicted(anom.Comp.SignalSound, anom, user);
        IncrementAnomalyCode(anom);
        return true;
    }

    public void IncrementAnomalyCode(Entity<ESPortalAnomalyComponent> ent)
    {
        ent.Comp.CodeIndex++;
        Dirty(ent);
        UpdateConsolesUi();

        if (ent.Comp.CodeIndex >= ent.Comp.CodeLength)
        {
            CollapseAnomaly(ent);
        }
        else
        {
            PlayAnomalyAnimation(ent);
        }
    }

    public void CollapseAnomaly(Entity<ESPortalAnomalyComponent> ent)
    {
        RaiseNetworkEvent(new ESAnomalyCollapseAnimationEvent
        {
            Anomaly = GetNetEntity(ent),
        });
        _timedDespawn.SetLifetime(ent.Owner, TimeSpan.FromSeconds(5));
    }

    public void PlayAnomalyAnimation(Entity<ESPortalAnomalyComponent> ent)
    {
        RaiseNetworkEvent(new ESAnomalyShrinkAnimationEvent
        {
            Anomaly = GetNetEntity(ent),
        });
    }

    public void PulseAnomalyRadiation(Entity<ESPortalAnomalyComponent> ent, EntityUid? user)
    {
        _audio.PlayPredicted(ent.Comp.RadPulseSound, ent, user);
        PredictedSpawnAttachedTo(ent.Comp.RadiationEntity, Transform(ent).Coordinates);
        RaiseNetworkEvent(new ESAnomalyRadiationAnimationEvent
        {
            Anomaly = GetNetEntity(ent),
        });
    }

    public void UpdateConsolesUi()
    {
        var query = EntityQueryEnumerator<ESAnomalyConsoleComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out var comp, out var ui))
        {
            UpdateUi((uid, comp, ui));
        }
    }

    public virtual void UpdateUi(Entity<ESAnomalyConsoleComponent?, UserInterfaceComponent?> ent)
    {

    }
}
