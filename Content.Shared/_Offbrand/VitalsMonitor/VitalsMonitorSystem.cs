using Content.Shared._Offbrand.Wounds;
using Content.Shared.Buckle.Components;
using Content.Shared.Buckle;
using Content.Shared.Chat;
using Content.Shared.DragDrop;
using Content.Shared.IdentityManagement;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Audio;
using Robust.Shared.Network;
using Robust.Shared.Timing;

namespace Content.Shared._Offbrand.VitalsMonitor;

public sealed class VitalsMonitorSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly INetManager _net = default!;
    [Dependency] private readonly SharedAppearanceSystem _appearance = default!;
    [Dependency] private readonly SharedAudioSystem _audio = default!;
    [Dependency] private readonly SharedChatSystem _chat = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedWoundableHealthAnalyzerSystem _woundableHealthAnalyzer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<VitalsMonitorComponent, CanDragEvent>(OnCanDrag);
        SubscribeLocalEvent<VitalsMonitorComponent, CanDropDraggedEvent>(OnCanDropDragged);
        SubscribeLocalEvent<VitalsMonitorComponent, DragDropDraggedEvent>(OnDragDropDragged);
        SubscribeLocalEvent<VitalsMonitorStrapComponent, CanDropTargetEvent>(OnCanDropTarget, after: [typeof(SharedBuckleSystem)]);
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<VitalsMonitorComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var vitalsMonitor, out var transform))
        {
            if (vitalsMonitor.Scanning is not { } target)
                continue;

            if (vitalsMonitor.NextUpdate > _timing.CurTime)
                continue;

            vitalsMonitor.NextUpdate = _timing.CurTime + vitalsMonitor.UpdateInterval;
            Dirty(uid, vitalsMonitor);

            var targetCoordinates = Transform(target).Coordinates;
            if (vitalsMonitor.MaxScanRange is { } scanRange && !_transform.InRange(targetCoordinates, transform.Coordinates, scanRange))
            {
                StopScanning((uid, vitalsMonitor));
                continue;
            }

            UpdateScanData((uid, vitalsMonitor), target);
            UpdateAppearance((uid, vitalsMonitor));
        }
    }

    private void OnCanDrag(Entity<VitalsMonitorComponent> ent, ref CanDragEvent args)
    {
        args.Handled = true;
    }

    private void OnCanDropDragged(Entity<VitalsMonitorComponent> ent, ref CanDropDraggedEvent args)
    {
        if (!(HasComp<HeartrateComponent>(args.Target) || HasComp<StrapComponent>(args.Target)))
            return;

        args.Handled = true;
        args.CanDrop = true;
    }

    private void OnDragDropDragged(Entity<VitalsMonitorComponent> ent, ref DragDropDraggedEvent args)
    {
        if (!(HasComp<HeartrateComponent>(args.Target) || HasComp<StrapComponent>(args.Target)))
            return;

        args.Handled = true;
        if (ent.Comp.Scanning == args.Target)
            StopScanning(ent);
        else
        {
            ent.Comp.Scanning = args.Target;
            var identity = Identity.Entity(args.Target, EntityManager);

            if (HasComp<HeartrateComponent>(args.Target))
                _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.ScanningPatient, ("patient", identity)), InGameICChatType.Speak, true);
            else
                _chat.TrySendInGameICMessage(ent, Loc.GetString(ent.Comp.ScanningStrap, ("strap", identity)), InGameICChatType.Speak, true);
        }

        Dirty(ent);
    }

    private void OnCanDropTarget(Entity<VitalsMonitorStrapComponent> ent, ref CanDropTargetEvent args)
    {
        if (HasComp<VitalsMonitorComponent>(args.Dragged))
        {
            args.CanDrop = true;
            args.Handled = true;
        }
    }

    // TODO: this should be defined in RT
    private bool AudioEquals(SoundSpecifier? a, SoundSpecifier? b)
    {
        return (a, b) switch {
            (SoundPathSpecifier pa, SoundPathSpecifier pb) => pa.Path == pb.Path,
            (SoundCollectionSpecifier ca, SoundCollectionSpecifier cb) => ca.Collection == cb.Collection,
            (null, null) => true,
            _ => false,
        };
    }

    private void UpdateScanData(Entity<VitalsMonitorComponent> ent, EntityUid target)
    {
        WoundableHealthAnalyzerData? data = null;

        if (_woundableHealthAnalyzer.TakeSample(target) is { } sample)
        {
            data = sample;
        }
        else if (TryComp<StrapComponent>(target, out var strap))
        {
            foreach (var buckled in strap.BuckledEntities)
            {
                if (_woundableHealthAnalyzer.TakeSample(buckled) is not { } sampled)
                    continue;

                data = sampled;
                break;
            }
        }

        ent.Comp.ScanData = data;
        Dirty(ent);
    }

    private void UpdateAppearance(Entity<VitalsMonitorComponent> ent)
    {
        if (ent.Comp.ScanData is not { } scanned)
        {
            ClearAppearance(ent);
            return;
        }

        // Brain activity
        if (ent.Comp.BrainActivityThresholds.LowestMatch(scanned.BrainHealth) is { } brainActivity)
            _appearance.SetData(ent, VitalsMonitorVisuals.BrainActivity, brainActivity);

        if (ent.Comp.BrainActivityWarningThresholds.LowestMatch(scanned.BrainHealth) is { } brainActivtyWarning)
            _appearance.SetData(ent, VitalsMonitorVisuals.BrainActivityWarning, brainActivtyWarning);
        else
            _appearance.SetData(ent, VitalsMonitorVisuals.BrainActivityWarning, false);

        // Breathing
        if (ent.Comp.BreathingThresholds.LowestMatch(scanned.RespiratoryRateModifier) is { } breathing)
            _appearance.SetData(ent, VitalsMonitorVisuals.Breathing, breathing);

        if (ent.Comp.BreathingWarningThresholds.LowestMatch(scanned.RespiratoryRateModifier) is { } breathingWarning)
            _appearance.SetData(ent, VitalsMonitorVisuals.BreathingWarning, breathingWarning);
        else
            _appearance.SetData(ent, VitalsMonitorVisuals.BreathingWarning, false);

        // Pulse
        if (scanned.HeartRate == 0)
        {
            SetAudio(ent, ent.Comp.AsystoleAudio);
            _appearance.SetData(ent, VitalsMonitorVisuals.Pulse, VitalsMonitorPulse.Asystole);
            _appearance.SetData(ent, VitalsMonitorVisuals.PulseWarning, true);
        }
        else
        {
            SetAudio(ent, ent.Comp.PulseAudioThresholds.HighestMatchClass(scanned.HeartStrain));

            if (ent.Comp.PulseThresholds.HighestMatch(scanned.HeartStrain) is { } pulse)
                _appearance.SetData(ent, VitalsMonitorVisuals.Pulse, pulse);

            if (ent.Comp.PulseWarningThresholds.HighestMatch(scanned.HeartStrain) is { } pulseWarning)
                _appearance.SetData(ent, VitalsMonitorVisuals.PulseWarning, pulseWarning);
            else
                _appearance.SetData(ent, VitalsMonitorVisuals.PulseWarning, false);
        }
    }

    private void ClearAppearance(Entity<VitalsMonitorComponent> ent)
    {
        _appearance.SetData(ent, VitalsMonitorVisuals.BrainActivity, VitalsMonitorBrainActivity.Blank);
        _appearance.SetData(ent, VitalsMonitorVisuals.BrainActivityWarning, false);
        _appearance.SetData(ent, VitalsMonitorVisuals.Breathing, VitalsMonitorBreathing.Blank);
        _appearance.SetData(ent, VitalsMonitorVisuals.BreathingWarning, false);
        _appearance.SetData(ent, VitalsMonitorVisuals.Pulse, VitalsMonitorPulse.Blank);
        _appearance.SetData(ent, VitalsMonitorVisuals.PulseWarning, false);
        SetAudio(ent, null);
    }

    private void StopScanning(Entity<VitalsMonitorComponent> ent)
    {
        ent.Comp.Scanning = null;
        Dirty(ent);

        ClearAppearance(ent);
    }

    private void SetAudio(Entity<VitalsMonitorComponent> ent, SoundSpecifier? audio)
    {
        if (_net.IsClient)
            return;

        if (AudioEquals(ent.Comp.CurrentAudio, audio))
            return;

        if (ent.Comp.LoopingAudio is { } looping)
        {
            ent.Comp.LoopingAudio = _audio.Stop(looping);
            Dirty(ent);
        }

        ent.Comp.CurrentAudio = audio;
        Dirty(ent);

        if (audio is not { } incomingSound)
            return;

        ent.Comp.LoopingAudio = _audio.PlayPvs(audio, ent, audio.Params.WithLoop(true))?.Entity;
        Dirty(ent);
    }
}
