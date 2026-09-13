using System.Linq;
using Content.Shared._ES.Forensics.Fingerprints.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.StationRecords;

namespace Content.Shared._ES.Forensics.Fingerprints;

public sealed partial class ESFingerprintsSystem
{
    private void InitializeCard()
    {
        SubscribeLocalEvent<ESFingerprintCardComponent, ExaminedEvent>(OnCardExamined);
        SubscribeLocalEvent<ESFingerprintCardComponent, MapInitEvent>(OnCardMapInit);
        SubscribeLocalEvent<ESFingerprintCardComponent, RefreshNameModifiersEvent>(OnCardRefreshNameModifiers);
        SubscribeLocalEvent<ESFingerprintCardComponent, UseInHandEvent>(OnCardUseInHand);
        SubscribeLocalEvent<ESFingerprintCardComponent, AfterInteractEvent>(OnCardAfterInteract);
        SubscribeLocalEvent<ESFingerprintCardComponent, ESMicroscopeGetReportEvent>(OnGetMicroscopeReport);
    }

    private void OnCardExamined(Entity<ESFingerprintCardComponent> ent, ref ExaminedEvent args)
    {
        using (args.PushGroup(nameof(ESFingerprintCardComponent), 2))
        {
            args.PushMarkup(ent.Comp.Used
                ? Loc.GetString("es-fingerprint-card-examine", ("count", ent.Comp.Fingerprints.Count))
                : Loc.GetString("es-fingerprint-card-examine-none"));

            args.PushMarkup(Loc.GetString("es-fingerprint-card-examine-usage"));
        }
    }

    private void OnCardMapInit(Entity<ESFingerprintCardComponent> ent, ref MapInitEvent args)
    {
        _nameModifier.RefreshNameModifiers(ent.Owner);
    }

    private void OnCardRefreshNameModifiers(Entity<ESFingerprintCardComponent> ent, ref RefreshNameModifiersEvent args)
    {
        if (!ent.Comp.Used)
            args.AddModifier("es-fingerprint-card-blank-prefix");
    }

    private void OnCardUseInHand(Entity<ESFingerprintCardComponent> ent, ref UseInHandEvent args)
    {
        if (ent.Comp.Used)
            return;
        args.Handled = true;

        if (!TryGetFingerprint(args.User, out var prints))
        {
            _popup.PopupEntity(Loc.GetString("es-fingerprint-card-popup-hands-covered"), args.User, args.User);
            return;
        }

        if (!TrySetCardFingerprints(ent.AsNullable(), prints.Value))
            return;

        _label.Label(ent, Name(args.User));
        _popup.PopupEntity(Loc.GetString("es-fingerprint-card-popup-stamped"), ent);
    }

    private void OnCardAfterInteract(Entity<ESFingerprintCardComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!TryComp<ESFingerprintCardComponent>(target, out var targetComp))
            return;

        // no interaction if one of the cards is obviously blank.
        if (!ent.Comp.Used || !targetComp.Used)
            return;

        args.Handled = true;

        if (targetComp.Fingerprints.ToHashSet().IsSupersetOf(ent.Comp.Fingerprints))
        {
            _popup.PopupEntity(Loc.GetString("es-fingerprint-card-popup-compare-match"), target);
        }
        else if (ent.Comp.Fingerprints.Intersect(targetComp.Fingerprints).Any())
        {
            _popup.PopupEntity(Loc.GetString("es-fingerprint-card-popup-compare-match-partial"), target);
        }
        else
        {
            _popup.PopupEntity(Loc.GetString("es-fingerprint-card-popup-compare-match-fail"), target);
        }
    }

    private void OnGetMicroscopeReport(Entity<ESFingerprintCardComponent> ent, ref ESMicroscopeGetReportEvent args)
    {
        if (_station.GetOwningStation(ent) is not { } station)
            return;

        args.Message.AddMarkupPermissive(Loc.GetString("es-fingerprint-report-header", ("count", ent.Comp.Fingerprints.Count)));
        args.Message.PushNewline();

        foreach (var fingerprint in ent.Comp.Fingerprints)
        {
            var found = false;
            foreach (var (_, record) in _stationRecords.GetRecordsOfType<GeneralStationRecord>(station))
            {
                if (record.Fingerprint != fingerprint)
                    continue;

                args.Message.AddMarkupPermissive(Loc.GetString("es-fingerprint-report-match", ("name", record.Name), ("job", record.JobTitle)));
                args.Message.PushNewline();
                found = true;
                break;
            }

            if (!found)
            {
                args.Message.AddMarkupPermissive(Loc.GetString("es-fingerprint-report-unknown"));
                args.Message.PushNewline();
            }
        }
    }

    /// <summary>
    /// Tries to set the fingerprints on a card, failing if it's already been set.
    /// </summary>
    public bool TrySetCardFingerprints(Entity<ESFingerprintCardComponent?> ent, params ESFingerprint[] fingerprints)
    {
        return TrySetCardFingerprints(ent, fingerprints.ToList());
    }

    /// <summary>
    /// Tries to set the fingerprints on a card, failing if it's already been set.
    /// </summary>
    public bool TrySetCardFingerprints(Entity<ESFingerprintCardComponent?> ent, IEnumerable<ESFingerprint> fingerprints)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;

        if (ent.Comp.Used)
            return false;

        ent.Comp.Fingerprints = fingerprints.ToList();
        _nameModifier.RefreshNameModifiers(ent.Owner);
        _appearance.SetData(ent, ESFingerprintCardVisuals.Used, true);
        Dirty(ent);

        return true;
    }
}
