using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.VendingMachines;
using Robust.Shared.GameStates;

namespace Content.Shared._ES.EntityTable.Components;

/// <summary>
/// Works with <see cref="VendingMachineComponent"/> to fill the inventory based on an entity table.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESEntityTableVendorFillSystem))]
public sealed partial class ESEntityTableVendorFillComponent : Component
{
    /// <summary>
    /// Items that will be added to <see cref="VendingMachineComponent.Inventory"/> on MapInit.
    /// </summary>
    [DataField]
    public EntityTableSelector Inventory = new NoneSelector();
}
