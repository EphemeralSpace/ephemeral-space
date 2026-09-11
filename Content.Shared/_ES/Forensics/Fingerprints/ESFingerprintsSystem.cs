using Content.Shared._ES.Forensics.Fingerprints.Components;
using Content.Shared.Examine;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Robust.Shared.Random;

namespace Content.Shared._ES.Forensics.Fingerprints;

public sealed partial class ESFingerprintsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESFingerprintsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESFingerprintsComponent, ContactInteractionEvent>(OnContactInteraction);

        SubscribeLocalEvent<ESFingerprintBlockerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESFingerprintBlockerComponent, InventoryRelayedEvent<ESTransferFingerprintsAttemptEvent>>(OnTransferFingerprintsAttempt);
    }

    private void OnMapInit(Entity<ESFingerprintsComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Fingerprint = new ESFingerprint(_random.Next());
        Dirty(ent);
    }

    private void OnContactInteraction(Entity<ESFingerprintsComponent> ent, ref ContactInteractionEvent args)
    {
        args.Handled = TryTransferFingerprints(ent.AsNullable(), args.Other);
    }

    private void OnExamined(Entity<ESFingerprintBlockerComponent> ent, ref ExaminedEvent args)
    {
        args.PushMarkup(Loc.GetString("es-fingerprint-blocker-examine"));
    }

    private void OnTransferFingerprintsAttempt(Entity<ESFingerprintBlockerComponent> ent, ref InventoryRelayedEvent<ESTransferFingerprintsAttemptEvent> args)
    {
        args.Args.Cancel();
    }

    /// <summary>
    /// Attempts to transfer an entity's fingerprints to the specified target as evidence
    /// </summary>
    public bool TryTransferFingerprints(Entity<ESFingerprintsComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        if (!CanTransferFingerprints(ent, target))
            return false;

        TransferFingerprints(ent, target);
        return true;
    }

    /// <summary>
    /// Transfers an entity's fingerprints to the specified target as evidence
    /// </summary>
    public void TransferFingerprints(Entity<ESFingerprintsComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var evidenceComponent = EnsureComp<ESFingerprintEvidenceComponent>(target);
        evidenceComponent.Fingerprints.Add(ent.Comp.Fingerprint);
        Dirty(target, evidenceComponent);
    }

    /// <summary>
    /// Checks if an entity's fingerprints can e transferred to a specific target as evidence
    /// </summary>
    public bool CanTransferFingerprints(Entity<ESFingerprintsComponent?> ent, EntityUid target)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        // TODO: component for blocking fingerprints on puddles


        var ev = new ESTransferFingerprintsAttemptEvent(ent, target);
        RaiseLocalEvent(ent, ref ev);

        return !ev.Cancelled;
    }
}

/// <summary>
/// Event raised on an entity with fingerprints when they touch another entity to check if fingerprints are transferred.
/// </summary>
[ByRefEvent]
public record struct ESTransferFingerprintsAttemptEvent(EntityUid User, EntityUid Target) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;

    public readonly EntityUid User = User;
    public readonly EntityUid Target = Target;

    public bool Cancelled { get; private set; } = false;

    public void Cancel()
    {
        Cancelled = true;
    }
}
