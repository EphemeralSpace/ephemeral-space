using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Weapons.Ranged.Attachments.Components;
using Content.Shared.Interaction;
using Content.Shared.Weapons.Ranged.Events;
using Content.Shared.Weapons.Ranged.Systems;
using Robust.Shared.Containers;

namespace Content.Shared._ES.Weapons.Ranged.Attachments;

public abstract class ESSharedGunAttachmentsSystem : EntitySystem
{
    [Dependency] private readonly SharedContainerSystem _container = default!;
    [Dependency] private readonly SharedGunSystem _gun = default!;

    private EntityQuery<ESGunAttachmentComponent> _attachmentQuery;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESAttachableGunComponent, EntInsertedIntoContainerMessage>(OnEntInsertedIntoContainer);
        SubscribeLocalEvent<ESAttachableGunComponent, EntRemovedFromContainerMessage>(OnEntRemovedFromContainer);
        SubscribeLocalEvent<ESAttachableGunComponent, GunRefreshModifiersEvent>(OnGunRefreshModifiers);

        SubscribeLocalEvent<ESGunAttachmentComponent, AfterInteractEvent>(OnAttachmentAfterInteract);

        _attachmentQuery = GetEntityQuery<ESGunAttachmentComponent>();
    }

    private void OnEntInsertedIntoContainer(Entity<ESAttachableGunComponent> ent, ref EntInsertedIntoContainerMessage args)
    {
        var containerId = args.Container.ID;
        if (!ent.Comp.Slots.Any(s => s.ContainerId.Equals(containerId)))
            return;
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnEntRemovedFromContainer(Entity<ESAttachableGunComponent> ent, ref EntRemovedFromContainerMessage args)
    {
        var containerId = args.Container.ID;
        if (!ent.Comp.Slots.Any(s => s.ContainerId.Equals(containerId)))
            return;
        _gun.RefreshModifiers(ent.Owner);
    }

    private void OnGunRefreshModifiers(Entity<ESAttachableGunComponent> ent, ref GunRefreshModifiersEvent args)
    {
        foreach (var attachment in EnumerateAttachments(ent))
        {
            RaiseLocalEvent(attachment, ref args);
        }
    }

    private void OnAttachmentAfterInteract(Entity<ESGunAttachmentComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Target is not { } target)
            return;

        if (!TryFindEmptyValidSlot(target, ent.AsNullable(), out var slot))
            return;

        args.Handled = TryInsertAttachment(target, ent.AsNullable(), slot.Value);
    }

    public bool HasAttachment(Entity<ESAttachableGunComponent> ent, ESGunAttachmentSlot slot)
    {
        return TryGetAttachment(ent, slot, out _);
    }

    public bool TryGetAttachment(Entity<ESAttachableGunComponent> ent, ESGunAttachmentSlot slot, [NotNullWhen(true)] out Entity<ESGunAttachmentComponent>? attachment)
    {
        attachment = null;
        if (!_container.TryGetContainer(ent, slot.ContainerId, out var container))
            return false;

        foreach (var contained in container.ContainedEntities)
        {
            if (!IsAttachmentValid(contained, slot))
                continue;
            attachment = (contained, _attachmentQuery.Get(contained));
            return true;
        }
        return false;
    }

    public bool IsAttachmentValid(Entity<ESGunAttachmentComponent?> ent, ESGunAttachmentSlot slot)
    {
        if (!_attachmentQuery.Resolve(ent, ref ent.Comp))
            return false;

        return slot.AttachmentTags.Intersect(ent.Comp.AttachmentTags).Any();
    }

    public bool TryFindEmptyValidSlot(Entity<ESAttachableGunComponent?> gun,
        Entity<ESGunAttachmentComponent?> attachment,
        [NotNullWhen(true)] out ESGunAttachmentSlot? outSlot)
    {
        outSlot = null;
        if (!Resolve(gun, ref gun.Comp) ||
            !Resolve(attachment, ref attachment.Comp))
            return false;

        foreach (var slot in gun.Comp.Slots)
        {
            // Slot is filled, can't be used.
            if (HasAttachment((gun, gun.Comp), slot))
                continue;

            if (!IsAttachmentValid(attachment, slot))
                continue;
            outSlot = slot;
            break;
        }

        return outSlot != null;
    }

    public bool TryInsertAttachment(Entity<ESAttachableGunComponent?> gun, Entity<ESGunAttachmentComponent?> attachment, ESGunAttachmentSlot slot)
    {
        if (!Resolve(gun, ref gun.Comp) ||
            !Resolve(attachment, ref attachment.Comp))
            return false;

        if (HasAttachment((gun, gun.Comp), slot) || !IsAttachmentValid(attachment, slot))
            return false;

        InsertAttachment(gun, attachment, slot);
        return true;
    }

    public void InsertAttachment(Entity<ESAttachableGunComponent?> gun, Entity<ESGunAttachmentComponent?> attachment, ESGunAttachmentSlot slot)
    {
        if (!Resolve(gun, ref gun.Comp) ||
            !Resolve(attachment, ref attachment.Comp))
            return;

        var container = _container.GetContainer(gun, slot.ContainerId);
        _container.Insert(attachment.Owner, container);
    }

    public IEnumerable<Entity<ESGunAttachmentComponent>> EnumerateAttachments(Entity<ESAttachableGunComponent> ent)
    {
        foreach (var slot in ent.Comp.Slots)
        {
            if (TryGetAttachment(ent, slot, out var attachment))
                yield return attachment.Value;
        }
    }
}
