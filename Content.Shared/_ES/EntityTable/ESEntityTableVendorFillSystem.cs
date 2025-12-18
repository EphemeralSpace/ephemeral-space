using System.Linq;
using Content.Shared._ES.EntityTable.Components;
using Content.Shared.EntityTable;
using Content.Shared.VendingMachines;

namespace Content.Shared._ES.EntityTable;

/// <summary>
/// This handles <see cref="ESEntityTableVendorFillComponent"/>
/// </summary>
public sealed class ESEntityTableVendorFillSystem : EntitySystem
{
    [Dependency] private readonly EntityTableSystem _entityTable = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESEntityTableVendorFillComponent, MapInitEvent>(OnMapInit);
    }

    private void OnMapInit(Entity<ESEntityTableVendorFillComponent> ent, ref MapInitEvent args)
    {
        if (!TryComp<VendingMachineComponent>(ent, out var vendingMachine))
        {
            throw new Exception($"{nameof(VendingMachineComponent)} not found, required for usage with {nameof(ESEntityTableVendorFillComponent)}");
        }

        var items = _entityTable.GetSpawns(ent.Comp.Inventory)
            .GroupBy(n => n)
            .Select(g => (g.Key, (uint) g.Count()));

        foreach (var (entProtoId, count) in items)
        {
            vendingMachine.Inventory.Add(entProtoId, new VendingMachineInventoryEntry(InventoryType.Regular, entProtoId, count));
        }
        Dirty(ent, vendingMachine);
    }
}
