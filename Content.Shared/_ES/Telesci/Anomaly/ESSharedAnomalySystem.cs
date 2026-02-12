using System.Linq;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Sparks;
using Content.Shared._ES.Telesci.Anomaly.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Popups;
using Content.Shared.Timing;
using Content.Shared.UserInterface;
using Content.Shared.Verbs;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Telesci.Anomaly;

public abstract class ESSharedAnomalySystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedDoAfterSystem _doAfter = default!;
    [Dependency] private readonly SharedPopupSystem _popup = default!;
    [Dependency] private readonly ESSparksSystem _sparks = default!;
    [Dependency] private readonly ESTimedDespawnSystem _timedDespawn = default!;
    [Dependency] private readonly UseDelaySystem _useDelay = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPortalAnomalyComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESPortalAnomalyComponent, ComponentShutdown>(OnShutdown);

        SubscribeLocalEvent<ESAnomalyResonatorComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESAnomalyResonatorComponent, AfterInteractEvent>(OnResonatorAfterInteract);
        SubscribeLocalEvent<ESAnomalyResonatorComponent, GetVerbsEvent<Verb>>(OnGetVerb);

        SubscribeLocalEvent<ESAnomalyProbeComponent, AfterInteractEvent>(OnProbeAfterInteract);
        SubscribeLocalEvent<ESAnomalyProbeComponent, ESProbeAnomalyDoAfterEvent>(OnProbeAnomalyDoAfter);

        SubscribeLocalEvent<ESAnomalyConsoleComponent, BeforeActivatableUIOpenEvent>(OnBeforeOpen);
    }

    private void OnExamined(Entity<ESAnomalyResonatorComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-anomaly-probe-mode-examine-signal", ("freq", GetSignalString(Loc, ent.Comp.CurrentSignal))));
    }

    private void OnResonatorAfterInteract(Entity<ESAnomalyResonatorComponent> ent, ref AfterInteractEvent args)
    {
        if (!TryComp<ESPortalAnomalyComponent>(args.Target, out var anom))
            return;

        if (_useDelay.IsDelayed(ent.Owner))
            return;

        if (TryUseSignal(ent, (args.Target.Value, anom), args.User))
            _useDelay.TryResetDelay(ent.Owner);
    }

    private void OnGetVerb(Entity<ESAnomalyResonatorComponent> ent, ref GetVerbsEvent<Verb> args)
    {
        if (!args.CanAccess || !args.CanInteract || !args.CanComplexInteract || args.Hands == null)
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
        if (!HasComp<ESPortalAnomalyComponent>(args.Target))
            return;

        _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
            args.User,
            ent.Comp.ProbeTime,
            new ESProbeAnomalyDoAfterEvent(),
            ent,
            args.Target.Value,
            ent)
        {
            DuplicateCondition = DuplicateConditions.None,
            BreakOnMove = false,
            NeedHand = true,
        });
        Dirty(ent);

        args.Handled = true;
    }

    private void OnProbeAnomalyDoAfter(Entity<ESAnomalyProbeComponent> ent, ref ESProbeAnomalyDoAfterEvent args)
    {
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

    private void OnBeforeOpen(Entity<ESAnomalyConsoleComponent> ent, ref BeforeActivatableUIOpenEvent args)
    {
        UpdateUi((ent, ent));
    }

    public void SetProbeSignal(Entity<ESAnomalyResonatorComponent> ent, ESAnomalySignal signal)
    {
        ent.Comp.CurrentSignal = signal;
        _appearance.SetData(ent, ESAnomalyResonatorVisuals.Mode, signal);
        Dirty(ent);
    }

    public static string GetSignalString(ILocalizationManager loc, ESAnomalySignal signal)
    {
        return loc.GetString($"es-anomaly-signal-{signal}");
    }

    public bool TryUseSignal(Entity<ESAnomalyResonatorComponent> probe, Entity<ESPortalAnomalyComponent> anom, EntityUid? user)
    {
        if (anom.Comp.CodeIndex == anom.Comp.CodeLength)
            return false;

        if (_timing.CurTime < anom.Comp.NextSignalTime)
            return false;

        var targetSignal = anom.Comp.SignalCode[anom.Comp.CodeIndex];

        if (probe.Comp.CurrentSignal != targetSignal)
        {
            PulseAnomalyRadiation(anom, user);
            return false;
        }

        _audio.PlayPredicted(anom.Comp.SignalSound, anom, user);
        IncrementAnomalyCode(anom, user);
        return true;
    }

    public void IncrementAnomalyCode(Entity<ESPortalAnomalyComponent> ent, EntityUid? user)
    {
        ent.Comp.CodeIndex++;
        Dirty(ent);
        UpdateConsolesUi();

        _popup.PopupPredicted(Loc.GetString("anomaly-popup-correct"), ent, user, PopupType.Medium);

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
        _popup.PopupPredicted(Loc.GetString("anomaly-popup-fail"), ent, user, PopupType.MediumCaution);
        PredictedSpawnAttachedTo(ent.Comp.RadiationEntity, Transform(ent).Coordinates);
        ent.Comp.NextSignalTime = _timing.CurTime + TimeSpan.FromSeconds(3);
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
