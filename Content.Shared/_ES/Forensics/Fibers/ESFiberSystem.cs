using Content.Shared._ES.Forensics.Fibers.Components;
using Content.Shared.DoAfter;
using Content.Shared.Hands.EntitySystems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;
using Content.Shared.Inventory;
using Content.Shared.Item;
using Content.Shared.Popups;
using Content.Shared.Random.Helpers;
using Content.Shared.Verbs;
using Robust.Shared.Random;

namespace Content.Shared._ES.Forensics.Fibers;

public sealed partial class ESFiberSystem : EntitySystem
{
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedHandsSystem _hands = default!;
    [Dependency] private InventorySystem _inventory = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedPopupSystem _popup = default!;

    // Additional chance to transfer fibers to entities that have ItemComponent
    // Taken arbitrarily from bay
    private const float ItemMultiplier = 1.2f;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESFiberClothingComponent, MapInitEvent>(OnMapInit);

        SubscribeLocalEvent<InventoryComponent, ContactInteractionEvent>(OnContactInteraction);

        SubscribeLocalEvent<ESFiberKitComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ESFiberKitComponent, GetVerbsEvent<UtilityVerb>>(OnGetVerbs);
        SubscribeLocalEvent<ESFiberKitComponent, ESCollectFiberDoAfterEvent>(OnCollectFiber);
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

    private void OnAfterInteract(Entity<ESFiberKitComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled || !args.CanReach || args.Target is not { } target)
            return;

        TryCollectFiber(ent, target, args.User);
        args.Handled = true;
        args.DoContactInteraction = false;
    }

    private void OnGetVerbs(Entity<ESFiberKitComponent> ent, ref GetVerbsEvent<UtilityVerb> args)
    {
        var user = args.User;
        var target = args.Target;
        args.Verbs.Add(new UtilityVerb
        {
            Text = Loc.GetString("es-fiber-kit-verb-collect"),
            IconEntity = GetNetEntity(ent),
            Disabled = !args.CanAccess || !args.CanInteract,
            DoContactInteraction = false,
            Act = () =>
            {
                TryCollectFiber(ent, target, user);
            },
        });
    }

    public bool TryCollectFiber(Entity<ESFiberKitComponent> ent, EntityUid target, EntityUid user)
    {
        if (!HasComp<ESFiberEvidenceComponent>(target))
        {
            _popup.PopupEntity(Loc.GetString("es-fiber-kit-popup-collect-none"), target, user);
            return false;
        }

        if (!_doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                user,
                ent.Comp.CollectTime,
                new ESCollectFiberDoAfterEvent(),
                ent,
                target,
                ent)
            {
                DuplicateCondition = DuplicateConditions.None,
                NeedHand = true,
                BreakOnMove = true,
            }))
            return false;

        _popup.PopupEntity(Loc.GetString("es-fiber-kit-popup-collect"), target);
        return true;
    }

    private void OnCollectFiber(Entity<ESFiberKitComponent> ent, ref ESCollectFiberDoAfterEvent args)
    {
        if (args.Cancelled || args.Target is not { } target)
            return;

        if (!TryComp<ESFiberEvidenceComponent>(target, out var evidence))
            return;

        // BUG: This will mispredict.
        var fiber = _random.PickAndTake(evidence.Evidence);
        Dirty(target, evidence);

        var fiberSpawn = PredictedSpawnNextToOrDrop(ent.Comp.FiberPrototype, args.User);
        _metaData.SetEntityName(fiberSpawn, Loc.GetString("es-fiber-entity-name", ("name", fiber.Appearance)));
        _hands.TryPickupAnyHand(args.User, fiberSpawn);

        // No more fibers to collect
        if (evidence.Evidence.Count == 0)
        {
            _popup.PopupEntity(Loc.GetString("es-fiber-kit-popup-collect-done"), target, args.User);
            RemComp<ESFiberEvidenceComponent>(target);
            return;
        }

        // More fibers, so repeat the search doafter
        _popup.PopupEntity(Loc.GetString("es-fiber-kit-popup-collect"), target);
        args.Repeat = true;
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
