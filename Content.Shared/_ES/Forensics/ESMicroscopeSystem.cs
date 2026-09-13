using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Breakable;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Forensics.Components;
using Content.Shared.Containers.ItemSlots;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Labels.Components;
using Content.Shared.Paper;
using Content.Shared.Popups;
using Content.Shared.Power.EntitySystems;
using Robust.Shared.Audio.Systems;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Forensics;

public sealed partial class ESMicroscopeSystem : EntitySystem
{
    [Dependency] private SharedAudioSystem _audio = default!;
    [Dependency] private ESBreakableSystem _breakable = default!;
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private ItemSlotsSystem _itemSlots = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private PaperSystem _paper = default!;
    [Dependency] private SharedPopupSystem _popup = default!;
    [Dependency] private SharedPowerReceiverSystem _powerReceiver = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEvidenceMicroscopeComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESEvidenceMicroscopeComponent, InteractHandEvent>(OnInteractHand);
        SubscribeLocalEvent<ESEvidenceMicroscopeComponent, ESMicroscopeScanTimerEvent>(OnMicroscopeScan);
    }

    private void OnExamined(Entity<ESEvidenceMicroscopeComponent> ent, ref ExaminedEvent args)
    {
        if (TryGetCurrentEvidence(ent.AsNullable(), out var evidence))
            args.PushMarkup(Loc.GetString("es-microscope-examine-evidence", ("object", evidence)));
        else
            args.PushMarkup(Loc.GetString("es-microscope-examine-evidence-none"));
    }

    private void OnInteractHand(Entity<ESEvidenceMicroscopeComponent> ent, ref InteractHandEvent args)
    {
        if (args.Handled)
            return;
        args.Handled = true;

        // No simultaneous scans
        if (ent.Comp.TimerEntity.HasValue)
            return;

        if (_breakable.IsBroken(ent.Owner) || !_powerReceiver.IsPowered(ent.Owner))
        {
            _popup.PopupEntity(Loc.GetString("es-microscope-popup-doesnt-work"), ent, args.User);
            return;
        }

        if (!TryGetCurrentEvidence(ent.AsNullable(), out _))
        {
            _popup.PopupEntity(Loc.GetString("es-microscope-popup-no-evidence"), ent, args.User);
            return;
        }

        _audio.PlayPredicted(ent.Comp.ScanSound, ent, args.User);
        _popup.PopupEntity(Loc.GetString("es-microscope-popup-scanning"), ent);

        // no removing evidence during analysis
        _itemSlots.SetLock(ent, ent.Comp.SlotId, true);
        ent.Comp.TimerEntity = _entityTimer.SpawnTimer(ent, ent.Comp.ScanTime, new ESMicroscopeScanTimerEvent());
        Dirty(ent);
    }

    private void OnMicroscopeScan(Entity<ESEvidenceMicroscopeComponent> ent, ref ESMicroscopeScanTimerEvent args)
    {
        _itemSlots.SetLock(ent, ent.Comp.SlotId, false);
        ent.Comp.TimerEntity = null;
        Dirty(ent);

        _popup.PopupEntity(Loc.GetString("es-microscope-popup-finished"), ent);

        if (!TryGetCurrentEvidence(ent.AsNullable(), out var evidence))
            return; // should never happen so don't bother with real message

        var msg = new FormattedMessage();
        msg.AddMarkupPermissive(Loc.GetString("es-microscope-report-title"));
        msg.PushNewline();
        msg.PushNewline();
        var ev = new ESMicroscopeGetReportEvent(msg);
        RaiseLocalEvent(evidence.Value, ref ev);

        var paper = PredictedSpawnNextToOrDrop(ent.Comp.ReportEntity, ent);
        var label = CompOrNull<LabelComponent>(evidence)?.CurrentLabel;
        _metaData.SetEntityName(paper,
            Loc.GetString("es-microscope-report-name",
            ("hasLabel", label != null),
            ("label", label!)));

        _paper.SetContent(paper, ev.Message.ToMarkup());
    }

    public bool TryGetCurrentEvidence(Entity<ESEvidenceMicroscopeComponent?> ent,
        [NotNullWhen(true)] out EntityUid? evidence)
    {
        evidence = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (!_itemSlots.TryGetSlot(ent, ent.Comp.SlotId, out var slot))
            return false;

        evidence = slot.Item;
        return evidence.HasValue;
    }
}

[ByRefEvent]
public readonly record struct ESMicroscopeGetReportEvent(FormattedMessage Message);
