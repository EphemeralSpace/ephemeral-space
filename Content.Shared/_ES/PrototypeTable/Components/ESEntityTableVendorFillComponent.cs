using Content.Shared.PrototypeTable.PrototypeSelectors;
using Content.Shared.VendingMachines;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.PrototypeTable.Components;

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
    public PrototypeTableSelector<EntityPrototype> Inventory = new NoneSelector<EntityPrototype>();
}
