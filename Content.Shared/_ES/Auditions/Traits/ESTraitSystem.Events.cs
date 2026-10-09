using Content.Shared.Inventory;
using Content.Shared.Mobs.Components;
using Content.Shared.Roles;
using Content.Shared.Station;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions.Traits;

public sealed partial class ESTraitSystem
{
    [Dependency] private SharedStationSpawningSystem _stationSpawning = default!;

    private void InitializeEvents()
    {
        SubscribeLocalEvent<MobStateComponent, ESAddComponentTraitEvent>(OnAddComponentTrait);
        SubscribeLocalEvent<InventoryComponent, ESStartingGearTraitEvent>(OnStartingGearTrait);
    }

    private void OnAddComponentTrait(Entity<MobStateComponent> ent, ref ESAddComponentTraitEvent args)
    {
        EntityManager.AddComponents(ent, args.Components);
    }

    private void OnStartingGearTrait(Entity<InventoryComponent> ent, ref ESStartingGearTraitEvent args)
    {
        _stationSpawning.EquipStartingGear(ent, args.Gear);
    }
}

public sealed partial class ESAddComponentTraitEvent : ESTraitEvent
{
    [DataField(required: true)]
    public ComponentRegistry Components = new();
}

public sealed partial class ESStartingGearTraitEvent : ESTraitEvent
{
    [DataField(required: true)]
    public ProtoId<StartingGearPrototype> Gear;
}
