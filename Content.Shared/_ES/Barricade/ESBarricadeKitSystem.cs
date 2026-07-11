using Content.Shared._ES.Barricade.Components;
using Content.Shared.DoAfter;
using Content.Shared.Doors.Components;
using Content.Shared.Doors.Systems;
using Content.Shared.Interaction;

namespace Content.Shared._ES.Barricade;

public sealed partial class ESBarricadeKitSystem : EntitySystem
{
    [Dependency] private SharedAirlockSystem _airlock = default!;
    [Dependency] private SharedDoAfterSystem _doAfter = default!;
    [Dependency] private SharedDoorSystem _door = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESBarricadeKitComponent, AfterInteractEvent>(OnAfterInteract);
        SubscribeLocalEvent<ESBarricadeKitComponent, ESBarricadeAirlockDoAfterEvent>(OnBarricadeAirlockDoAfter);
        SubscribeLocalEvent<ESBarricadeKitComponent, DoAfterAttemptEvent<ESBarricadeAirlockDoAfterEvent>>(OnBarricadeAirlockDoAfterAttempt);
    }

    private void OnAfterInteract(Entity<ESBarricadeKitComponent> ent, ref AfterInteractEvent args)
    {
        if (args.Handled)
            return;

        if (args.Target.HasValue && CanBarricade(args.Target.Value))
        {
            args.Handled = _doAfter.TryStartDoAfter(new DoAfterArgs(EntityManager,
                args.User,
                ent.Comp.SetupDelay,
                new ESBarricadeAirlockDoAfterEvent(),
                ent,
                args.Target.Value,
                ent)
            {
                BreakOnDamage = true,
                BreakOnHandChange = true,
                NeedHand = true,
                DuplicateCondition = DuplicateConditions.SameTool,
                AttemptFrequency = AttemptFrequency.EveryTick,
            });
        }
    }

    private void OnBarricadeAirlockDoAfter(Entity<ESBarricadeKitComponent> ent, ref ESBarricadeAirlockDoAfterEvent args)
    {
        if (args.Handled || args.Cancelled || args.Target is not { } target)
            return;

        PredictedSpawnAtPosition(ent.Comp.AirlockBarricade, Transform(target).Coordinates);
        PredictedQueueDel(ent);
        args.Handled = true;
    }

    private void OnBarricadeAirlockDoAfterAttempt(Entity<ESBarricadeKitComponent> ent, ref DoAfterAttemptEvent<ESBarricadeAirlockDoAfterEvent> args)
    {
        if (args.Event.Target is null || !CanBarricade(args.Event.Target.Value))
            args.Cancel();
    }

    private bool CanBarricade(EntityUid target)
    {
        return HasComp<AirlockComponent>(target) &&
               _door.GetDoorState(target) == DoorState.Closed &&
               !_airlock.IsBarricaded(target);
    }
}
