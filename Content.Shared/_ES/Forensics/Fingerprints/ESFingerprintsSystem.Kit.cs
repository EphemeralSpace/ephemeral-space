using Content.Shared._ES.Forensics.Fingerprints.Components;
using Content.Shared.DoAfter;
using Content.Shared.Interaction;

namespace Content.Shared._ES.Forensics.Fingerprints;

public sealed partial class ESFingerprintsSystem
{
    private void InitializeKit()
    {
        SubscribeLocalEvent<ESFingerprintKitComponent, AfterInteractEvent>(OnKitAfterInteract);
        SubscribeLocalEvent<ESFingerprintKitComponent, ESDustFingerprintsDoAfterEvent>(OnDustFingerprintsDoAfter);
    }

    private void OnKitAfterInteract(Entity<ESFingerprintKitComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.DustTime,
                new ESDustFingerprintsDoAfterEvent(),
                args.Used,
                target,
                args.Used)
            {
                BreakOnDamage = true,
                BreakOnMove = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTool,
            }))
            return;

        _popup.PopupEntity(Loc.GetString("es-fingerprint-kit-popup-dust-start"), target);
        args.Handled = true;
    }

    private void OnDustFingerprintsDoAfter(Entity<ESFingerprintKitComponent> ent, ref ESDustFingerprintsDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!TryGetFingerprintEvidence(target, out var fingerprints))
        {
            _popup.PopupEntity(Loc.GetString("es-fingerprint-kit-popup-no-prints"), target);
            return;
        }

        args.Handled = true;

        var card = PredictedSpawnNextToOrDrop(ent.Comp.CardPrototype, args.User);
        TrySetCardFingerprints(card, fingerprints);
        _label.Label(card, Name(target));
        _hands.TryPickupAnyHand(args.User, card);
        _popup.PopupEntity(Loc.GetString("es-fingerprint-kit-popup-prints"), target);
    }
}
