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

    // Additional chance to transfer fibers to entities that have ItemComponent
    // Taken arbitrarily from bay
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
        args.Handled = true;
    }

    /// <summary>
    /// Attempts to transfer a random amount of worn clothing fibers from <see cref="user"/> to <see cref="target"/>.
    /// </summary>
    public bool TryTransferFiber(EntityUid target, EntityUid user)
    {
        if (!TryGetRandomClothingFibers(target, user, out var fibers))
            return false;

        var comp = EnsureComp<ESFiberEvidenceComponent>(target);
        foreach (var fiber in fibers)
        {
            comp.Evidence.Add(fiber);
        }
        Dirty(target, comp);
        return true;
    }

    /// <summary>
    /// Retrieves a random amount of clothing fibers from the <see cref="user"/> entity's worn items.
    /// Non-deterministic and based on the random transfer chance per clothing item.
    /// </summary>
    public bool TryGetRandomClothingFibers(EntityUid target, EntityUid user, out HashSet<ESFiber> fibers)
    {
        fibers = new HashSet<ESFiber>();

        var itemMultiplier = HasComp<ItemComponent>(target) ? ItemMultiplier : 1f;
        foreach (var slotEntity in _inventory.GetSlotEntities(user, SlotFlags.WITHOUT_POCKET))
        {
            if (!TryComp<ESFiberClothingComponent>(slotEntity, out var comp))
                continue;

            if (!_random.Prob(comp.TransferChance * itemMultiplier))
                continue;

            fibers.Add(GetFiber((slotEntity, comp)));
        }

        return fibers.Count != 0;
    }

    /// <summary>
    /// Constructs a fiber based on an entity
    /// </summary>
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
