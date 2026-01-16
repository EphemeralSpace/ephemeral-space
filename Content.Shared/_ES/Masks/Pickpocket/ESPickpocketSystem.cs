using Content.Shared._Citadel.Utilities;
using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Hands.Components;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Content.Shared.Storage;
using Content.Shared.Storage.EntitySystems;
using Robust.Shared.Containers;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Pickpocket;

public sealed class ESPickpocketSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedContainerSystem _containerSystem = default!;
    [Dependency] private readonly SharedDoAfterSystem _doafterSystem = default!;
    [Dependency] private readonly SharedHandsSystem _handsSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedStorageSystem _storageSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESPickpocketerComponent, ComponentStartup>(OnComponentStartup);
        SubscribeLocalEvent<ESPickpocketerComponent, ESPickpocketActionEvent>(OnPickpocketAction);
        SubscribeLocalEvent<ESPickpocketerComponent, ESPickpocketDoAfterEvent>(OnDoAfter);
    }

    private void OnComponentStartup(Entity<ESPickpocketerComponent> ent, ref ComponentStartup args)
    {
        ent.Comp.Rng = new SmallRandom(_random);
        Dirty(ent);
    }

    private void OnPickpocketAction(Entity<ESPickpocketerComponent> ent, ref ESPickpocketActionEvent args)
    {
        if (args.Handled)
            return;

        var target = args.Target;
        var selfRot = _transformSystem.GetWorldRotation(ent).GetCardinalDir();
        var targetRot = _transformSystem.GetWorldRotation(args.Target).GetCardinalDir();

        if (selfRot != targetRot)
        {
            _popupSystem.PopupClient("Must be facing your target's back!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        if (!TryComp(target, out MobStateComponent? targetState) ||
            targetState.CurrentState != MobState.Alive)
        {
            _popupSystem.PopupClient("Target must be conscious!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        if (!TryComp(target, out InventoryComponent? inventory))
        {
            _popupSystem.PopupClient("Target must have an inventory!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        // :frost:
        if (!_inventorySystem.TryGetSlotEntity(target, "back", out var held, inventory) &&
            !HasComp<StorageComponent>(held))
        {
            _popupSystem.PopupClient("Target must have a backpack!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        args.Handled = true;

        _popupSystem.PopupClient("Attempting pickpocket...", ent.Owner, ent.Owner);

        var doafterArgs = new DoAfterArgs(EntityManager, ent.Owner, 5, new ESPickpocketDoAfterEvent(), ent.Owner, target: args.Target, used: ent.Owner)
        {
            Hidden = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true,
            DuplicateCondition = DuplicateConditions.All,
            NeedHand = true,
            BreakOnHandChange = true,
        };

        _doafterSystem.TryStartDoAfter(doafterArgs);
    }

    private void OnDoAfter(Entity<ESPickpocketerComponent> ent, ref ESPickpocketDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || !args.Target.HasValue ||
            !TryComp(args.Target, out InventoryComponent? inventory) ||
            !_inventorySystem.TryGetSlotEntity(args.Target.Value, "back", out var held, inventory) ||
            !TryComp<StorageComponent>(held, out var storage)
        )
            return;

        if (storage.Container.ContainedEntities.Count == 0)
        {
            _popupSystem.PopupClient("Target had no items in their bag!", ent.Owner, ent.Owner, PopupType.MediumCaution);
            return;
        }

        var items = storage.Container.ContainedEntities;
        var chosenItem = ent.Comp.Rng.Pick(items);

        if (!TryComp<HandsComponent>(args.User, out var hands))
            return;

        // _containerSystem.TryRemoveFromContainer(chosenItem, force: true);
        _handsSystem.PickupOrDrop(args.User, chosenItem, handsComp: hands);
    }
}

public sealed partial class ESPickpocketActionEvent : EntityTargetActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class ESPickpocketDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
