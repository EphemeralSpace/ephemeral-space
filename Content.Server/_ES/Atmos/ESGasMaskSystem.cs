using Content.Server.Body.Systems;
using Content.Shared._ES.Atmos;
using Content.Shared._ES.Atmos.Components;
using Content.Shared.Inventory;

namespace Content.Server._ES.Atmos;

/// <inheritdoc/>
public sealed partial class ESGasMaskSystem : ESSharedGasMaskSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESGasMaskComponent, InventoryRelayedEvent<ESModifyInhaledGasEvent>>(OnModifyInhaledGas);
    }

    private void OnModifyInhaledGas(Entity<ESGasMaskComponent> ent, ref InventoryRelayedEvent<ESModifyInhaledGasEvent> args)
    {
        foreach (var gas in ent.Comp.BlockedGases)
        {
            args.Args.Gas.SetMoles(gas, 0);
        }
    }
}
