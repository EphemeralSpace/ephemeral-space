using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;
using Content.Shared.Popups;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Pickpocket;

public sealed class ESPickpocketSystem : EntitySystem
{
    [Dependency] private readonly SharedDoAfterSystem _doafterSystem = default!;
    [Dependency] private readonly InventorySystem _inventorySystem = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;
    [Dependency] private readonly SharedTransformSystem _transformSystem = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESPickpocketerComponent, ESPickpocketActionEvent>(OnPickpocketAction);
        SubscribeLocalEvent<ESPickpocketerComponent, ESPickpocketDoAfterEvent>(OnDoAfter);
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

        if (!TryComp(target, out InventoryComponent? inventory) ||
            !_inventorySystem.TryGetSlotEntity(target, "Backpack", out EntityUid? held, inventory))
        {
            _popupSystem.PopupClient("Target must have a backpack!", ent.Owner, PopupType.MediumCaution);
            return;
        }

        args.Handled = true;

        _popupSystem.PopupClient("Attempting pickpocket...", ent.Owner, ent.Owner);

        var doafterArgs = new DoAfterArgs(EntityManager, ent.Owner, 2, new ESPickpocketDoAfterEvent(), ent.Owner, target: args.Target, used: ent.Owner)
        {
            Hidden = true,
            BreakOnMove = true,
            BreakOnDamage = true,
            BreakOnWeightlessMove = true,
            BlockDuplicate = true
        };

        _doafterSystem.TryStartDoAfter(doafterArgs);
    }

    private void OnDoAfter(Entity<ESPickpocketerComponent> ent, ref ESPickpocketDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled)
            return;

        _popupSystem.PopupClient("Pickpocket completed!", ent.Owner, ent.Owner);
    }
}

public sealed partial class ESPickpocketActionEvent : EntityTargetActionEvent
{

}

[Serializable, NetSerializable]
public sealed partial class ESPickpocketDoAfterEvent : SimpleDoAfterEvent;
