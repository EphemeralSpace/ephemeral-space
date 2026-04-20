using System.Diagnostics.CodeAnalysis;
using Content.Server._ES.Masks.Pickpocket.Components;
using Content.Server.DoAfter;
using Content.Server.Hands.Systems;
using Content.Server.Mind;
using Content.Server.Popups;
using Content.Shared._ES.Masks.Pickpocket;
using Content.Shared._ES.Viewcone;
using Content.Shared.ActionBlocker;
using Content.Shared.DoAfter;
using Content.Shared.Inventory;
using Content.Shared.Storage;
using Robust.Server.Containers;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Pickpocket;

public sealed class ESPickpocketMaskSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ActionBlockerSystem _actionBlocker = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DoAfterSystem _doAfter = default!;
    [Dependency] private readonly HandsSystem _hands = default!;
    [Dependency] private readonly InventorySystem _inventory = default!;
    [Dependency] private readonly MindSystem _mind = default!;
    [Dependency] private readonly PopupSystem _popup = default!;
    [Dependency] private readonly ESViewconeAngleSystem _viewconeAngle = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESPickpocketTargetActionEvent>(OnPickpocketTargetAction);
        SubscribeLocalEvent<DoAfterComponent, ESPickpocketTargetDoAfterEvent>(OnPickpocketTargetDoAfter);
        SubscribeLocalEvent<DoAfterAttemptEvent<ESPickpocketTargetDoAfterEvent>>(OnDoAfterAttempt);
    }

    private void OnPickpocketTargetAction(ESPickpocketTargetActionEvent args)
    {
        if (!_actionBlocker.CanInteract(args.Target, null))
            return;

        if (_viewconeAngle.InViewcone(args.Target, args.Performer))
        {
            _popup.PopupEntity(Loc.GetString("es-pickpocket-action-in-view"), args.Target, args.Performer);
            return;
        }

        if (TryComp<ESPickpocketedMarkerComponent>(args.Target, out var comp) &&
            _mind.TryGetMind(args.Performer, out var mind) &&
            comp.PickpocketMinds.Contains(mind.Value))
        {
            _popup.PopupEntity(Loc.GetString("es-pickpocket-action-already-pickpocketed"), args.Target, args.Performer);
            return;
        }

        if (!TryGetBag(args.Target, out _))
        {
            _popup.PopupEntity(Loc.GetString("es-pickpocket-action-no-bag"), args.Target, args.Performer);
            return;
        }

        args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(
            EntityManager,
            args.Performer,
            args.Delay,
            new ESPickpocketTargetDoAfterEvent(),
            args.Performer,
            args.Target)
        {
            AttemptFrequency = AttemptFrequency.EveryTick,
            DuplicateCondition = DuplicateConditions.SameEvent,
            BreakOnMove = true,
            Hidden = true,
        });
    }

    private void OnPickpocketTargetDoAfter(Entity<DoAfterComponent> ent, ref ESPickpocketTargetDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!TryGetBag(target, out var bag))
            return;

        // edge case: we pickpocketed an empty bag
        if (bag.Value.Comp.Container.ContainedEntities.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("es-pickpocket-action-empty"), target, args.User);
            return;
        }

        var item = _random.Pick(bag.Value.Comp.Container.ContainedEntities);
        if (!_container.Remove(item, bag.Value.Comp.Container))
            return;

        var comp = EnsureComp<ESPickpocketStolenComponent>(item);
        if (_mind.TryGetMind(args.User, out var userMind))
        {
            comp.StealerMinds.Add(userMind.Value);
            var markerComp = EnsureComp<ESPickpocketedMarkerComponent>(target);
            markerComp.PickpocketMinds.Add(userMind.Value);
        }

        if (_mind.TryGetMind(target, out var targetMind))
        {
            comp.StolenMinds.Add(targetMind.Value);
        }

        _hands.TryPickupAnyHand(args.User, item, animate: false);
    }

    private void OnDoAfterAttempt(DoAfterAttemptEvent<ESPickpocketTargetDoAfterEvent> args)
    {
        if (args.Event.Target is not { } target ||
            _viewconeAngle.InViewcone(target, args.Event.User) ||
            !TryGetBag(target, out _))
        {
            args.Cancel();
        }
    }

    private bool TryGetBag(Entity<InventoryComponent?> ent, [NotNullWhen(true)] out Entity<StorageComponent>? bag)
    {
        bag = null;

        if (!Resolve(ent, ref ent.Comp, false))
            return false;

        // hardcoded string is evil but this is just how it's done in this hellworld.
        if (!_inventory.TryGetSlotEntity(ent, "back", out var slotEntity, ent))
            return false;

        if (!TryComp<StorageComponent>(slotEntity, out var storageComponent))
            return false;

        bag = (slotEntity.Value, storageComponent);
        return true;
    }
}
