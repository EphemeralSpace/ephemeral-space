using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Forensics.Fingerprints.Components;
using Content.Shared.DoAfter;
using Content.Shared.Examine;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Labels.EntitySystems;
using Content.Shared.NameModifier.EntitySystems;
using Content.Shared.Popups;
using Robust.Shared.Random;

namespace Content.Shared._ES.Forensics.Fingerprints;

public sealed partial class ESFingerprintsSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedAppearanceSystem _appearance = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private LabelSystem _label = default!;
    [Dependency] private NameModifierSystem _nameModifier = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESFingerprintsComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESFingerprintsComponent, ContactInteractionEvent>(OnContactInteraction);

        SubscribeLocalEvent<ESFingerprintBlockerComponent, ExaminedEvent>(OnExamined);
        SubscribeLocalEvent<ESFingerprintBlockerComponent, InventoryRelayedEvent<ESTransferFingerprintsAttemptEvent>>(OnTransferFingerprintsAttempt);

        InitializeCard();
        InitializeKit();
    }

    private void OnMapInit(Entity<ESFingerprintsComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.Fingerprint = new ESFingerprint(_random.Next());
        Dirty(ent);
    }

    private void OnContactInteraction(Entity<ESFingerprintsComponent> ent, ref ContactInteractionEvent args)
    {
        // Do not leave fingerprints if we are not actually making direct contact with an object.
        if (args.Used is not null)
            return;

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
        // TODO: component for blocking fingerprints on puddles

        return TryGetFingerprints(ent, out _);
    }

    /// <summary>
    /// Attempts to retrieve the fingerprints off an entity
    /// </summary>
    /// <param name="ent">The entity to get prints from</param>
    /// <param name="prints">The fingerprints</param>
    /// <param name="ignoreBlockers">If true, ignore things like gloves which obscure fingerprints</param>
    /// <returns>Whether the fingerprints were retrieved successfully</returns>
    public bool TryGetFingerprints(
        Entity<ESFingerprintsComponent?> ent,
        [NotNullWhen(true)] out ESFingerprint? prints,
        bool ignoreBlockers = false)
    {
        prints = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        prints = ent.Comp.Fingerprint;

        if (!ignoreBlockers)
        {
            var ev = new ESTransferFingerprintsAttemptEvent(ent);
            RaiseLocalEvent(ent, ref ev);

            return !ev.Cancelled;
        }

        return true;
    }

    /// <summary>
    /// Tries to get the fingerprints left on an entity as evidence.
    /// Fails if there are none.
    /// </summary>
    public bool TryGetFingerprintEvidence(
        Entity<ESFingerprintEvidenceComponent?> ent,
        [NotNullWhen(true)] out HashSet<ESFingerprint>? fingerprints)
    {
        fingerprints = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        fingerprints = ent.Comp.Fingerprints;
        return fingerprints.Count != 0;
    }
}

/// <summary>
/// Event raised on an entity with fingerprints when they touch another entity to check if fingerprints are transferred.
/// </summary>
[ByRefEvent]
public record struct ESTransferFingerprintsAttemptEvent(EntityUid User) : IInventoryRelayEvent
{
    public SlotFlags TargetSlots { get; } = SlotFlags.GLOVES;

    public readonly EntityUid User = User;

    public bool Cancelled { get; private set; } = false;

    public void Cancel()
    {
        Cancelled = true;
    }
}
