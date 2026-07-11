using Content.Shared._ES.Barricade.Components;
using Content.Shared.Charges.Systems;
using Content.Shared.Coordinates.Helpers;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;
using Content.Shared.Interaction.Events;

namespace Content.Shared._ES.Barricade;

public sealed partial class ESBarricadeKitSystem : EntitySystem
{
    [Dependency] private SharedAirlockSystem _airlock = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedDoorSystem _door = default!;
    [Dependency] private SharedChargesSystem _charge = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBarricadeKitComponent, UseInHandEvent>(OnUseInHand);
        SubscribeLocalEvent<ESBarricadeKitComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ESBarricadeKitComponent, ESBarricadeDoAfterEvent>(OnBarricadeDoAfter);
        SubscribeLocalEvent<ESBarricadeKitComponent, DoAfterAttemptEvent<ESBarricadeDoAfterEvent>>(OnBarricadeDoAfterAttempt);
    }

    private void OnUseInHand(Entity<ESBarricadeKitComponent> ent, ref UseInHandEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartDoafter(ent, ESBarricadeKitBarricadeType.Tile, args.User, null);
    }

    private void OnAfterInteract(Entity<ESBarricadeKitComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = StartDoafter(ent, ESBarricadeKitBarricadeType.Airlock, args.User, args.Target);
    }

    private bool StartDoafter(Entity<ESBarricadeKitComponent> ent, ESBarricadeKitBarricadeType type, EntityUid user, EntityUid? target)
    {
        if (target.HasValue && !CanBarricade(target.Value))
            return false;

        return _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                user,
                ent.Comp.SetupDelay,
                new ESBarricadeDoAfterEvent() { Type = type },
                ent,
                target ?? user,
                ent)
            {
                BreakOnDamage = true,
                BreakOnHandChange = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTool,
                AttemptFrequency = AttemptFrequency.EveryTick,
            });
    }

    private void OnBarricadeDoAfter(Entity<ESBarricadeKitComponent> ent, ref ESBarricadeDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        var proto = args.Type == ESBarricadeKitBarricadeType.Airlock
            ? ent.Comp.AirlockBarricade
            : ent.Comp.TileBarricade;

        PredictedSpawnAtPosition(proto, Transform(target).Coordinates.SnapToGrid(entMan: EntityManager));
        _charge.TryUseCharge(ent.Owner);
        args.Handled = true;
    }

    private void OnBarricadeDoAfterAttempt(Entity<ESBarricadeKitComponent> ent, ref DoAfterAttemptEvent<ESBarricadeDoAfterEvent> args)
    {
        if (args.Event.Type == ESBarricadeKitBarricadeType.Airlock && (args.Event.Target is null || !CanBarricade(args.Event.Target.Value)))
            args.Cancel();
    }

    private bool CanBarricade(EntityUid target)
    {
        return HasComp<AirlockComponent>(target) &&
               _door.GetDoorState(target) == DoorState.Closed &&
               !_airlock.IsBarricaded(target);
    }
}
