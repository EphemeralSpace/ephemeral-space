using Content.Shared._ES.Forensics.Fibers.Components;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Robust.Shared.Random;

namespace Content.Shared._ES.Forensics.Fibers;

public sealed partial class ESFiberSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private InventorySystem _inventory = default!;

    private const float ItemMultiplier = 1.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESFiberClothingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<InventoryComponent, ContactInteractionEvent>(OnContactInteraction);
    }

    private void OnMapInit(Entity<ESFiberClothingComponent> ent, ref MapInitEvent args)
    {
        ent.Comp.FiberId = _random.Next();
        Dirty(ent);
    }

    private void OnContactInteraction(Entity<InventoryComponent> ent, ref ContactInteractionEvent args)
    {
        TryTransferFiber(args.Other, ent);
    }

    public bool TryTransferFiber(EntityUid target, EntityUid user)
    {
        if (!TryGetRandomClothingFiber(target, user, out var fibers))
            return false;

        var comp = EnsureComp<ESFiberEvidenceComponent>(target);
        foreach (var fiber in fibers)
        {
            comp.Evidence.Add(fiber);
        }
        Dirty(target, comp);
        return true;
    }

    public bool TryGetRandomClothingFiber(EntityUid target, EntityUid user, out HashSet<ESFiber> fibers)
    {
        fibers = new HashSet<ESFiber>();

        foreach (var slotEntity in _inventory.GetSlotEntities(user, SlotFlags.WITHOUT_POCKET))
        {
            if (!TryComp<ESFiberClothingComponent>(slotEntity, out var comp))
                continue;

            var itemMultiplier = HasComp<ItemComponent>(target) ? ItemMultiplier : 1f;
            if (!_random.Prob(comp.TransferChance * itemMultiplier))
                continue;

            fibers.Add(GetFiber((slotEntity, comp)));
        }

        return fibers.Count != 0;
    }

    public ESFiber GetFiber(Entity<ESFiberClothingComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return default;

        var appearance = ent.Comp.PhysicalDescription.HasValue
            ? Loc.GetString(ent.Comp.PhysicalDescription)
            : Name(ent);
        return new ESFiber(appearance, ent.Comp.FiberId);
    }
}
