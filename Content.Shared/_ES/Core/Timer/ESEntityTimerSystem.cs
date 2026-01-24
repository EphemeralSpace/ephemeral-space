using Content.Shared._ES.Core.Timer.Components;
using JetBrains.Annotations;
using Robust.Shared.Collections;
using Robust.Shared.Map;
using Robust.Shared.Serialization;
using Robust.Shared.Timing;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Core.Timer;

/// <summary>
/// Used for creating generic timers which serialize to the world.
/// </summary>
public sealed class ESEntityTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IComponentFactory _factory = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<MethodTimerEvent>(MethodTimerEventHandler);
    }

    private void MethodTimerEventHandler(MethodTimerEvent ev)
    {
        ev.Method();
    }

    /// <summary>
    ///     Spawns a timer entity that raises a broadcast event after a specified duration.
    /// </summary>
    /// <param name="duration">Duration of the timer</param>
    /// <param name="endEvent">Event that will be raised when the timer is finished</param>
    /// <param name="logFailure">Whether to log if SpawnTimer fails to spawn the timer.</param>
    /// <returns>The timer that was created</returns>
    [PublicAPI]
    [MustUseReturnValue]
    public Entity<ESEntityTimerComponent>? SpawnTimer(TimeSpan duration, ESEntityTimerEvent endEvent, bool logFailure = true)
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);

        return SetupTimer(uid, duration, endEvent);
    }

    /// <summary>
    ///     Spawns a timer entity that raises a directed event on a target after a specified duration.
    /// </summary>
    /// <param name="target">Entity the event will raise on</param>
    /// <param name="duration">Duration of the timer</param>
    /// <param name="endEvent">Event that will be raised when the timer is finished</param>
    /// <param name="logFailure">Whether to log if SpawnTimer fails to spawn the timer.</param>
    /// <returns>The timer that was created</returns>
    [PublicAPI]
    [MustUseReturnValue]
    public Entity<ESEntityTimerComponent>? SpawnTimer(EntityUid target, TimeSpan duration, ESEntityTimerEvent endEvent, bool logFailure = true)
    {
        if (!TimerTargetIsValid(target))
        {
            if (logFailure)
            {
                if (TerminatingOrDeleted(target))
                    Log.Error($"Failed to spawn a timer on {target} due to being in the middle of termianting/being deleted, event was {endEvent}.");
                else if (LifeStage(target) is not EntityLifeStage.MapInitialized)
                    Log.Error($"Failed to spawn a timer on {target} due to not being map initialized (was {LifeStage(target)}), event was {endEvent}.");
            }

            return null;
        }

        var uid = Spawn();

        _transform.SetParent(uid, target);

        return SetupTimer(uid, duration, endEvent);
    }

    /// <summary>
    ///     Spawns a method timer, which is <b>not networked</b> and <b>not serializable</b>, but convenient in some
    ///     select usecases. Do not use for things that need prediction, do not use for things that need to be saved
    ///     in maps/etc.
    /// </summary>
    /// <param name="duration">Duration of the timer</param>
    /// <param name="method">The lambda to call when the timer has elapsed.</param>
    /// <param name="logFailure">Whether to log if SpawnMethodTimer fails to spawn the timer.</param>
    /// <returns>The timer that was created</returns>
    [PublicAPI]
    [MustUseReturnValue]
    public Entity<ESEntityTimerComponent>? SpawnMethodTimer(TimeSpan duration, Action method, bool logFailure = true)
    {
        return SpawnTimer(duration, new MethodTimerEvent(method), logFailure);
    }

    private Entity<ESEntityTimerComponent> SetupTimer(EntityUid timerEnt, TimeSpan duration, ESEntityTimerEvent endEvent)
    {
        var comp = _factory.GetComponent<ESEntityTimerComponent>();

        comp.TimerEndEvent = endEvent;
        comp.TimerEnd = _timing.CurTime + duration;

        // This is essentially only checking that the type is net serializable, nothing more.
        // If it's not, then we never dirty the component on purpose and disable netsync.
        var networked = endEvent.GetType().HasCustomAttribute<NetSerializableAttribute>() &&
                        !endEvent.GetType().HasCustomAttribute<NonNetworkedTimerEventAttribute>();

        comp.NetSyncEnabled = networked;

        AddComp(timerEnt, comp);

        if (networked)
        {
            Dirty(timerEnt, comp);
        }

        return (timerEnt, comp);
    }

    private bool TimerTargetIsValid(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) && LifeStage(uid) == EntityLifeStage.MapInitialized;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var firingTimers = new ValueList<Entity<ESEntityTimerComponent, TransformComponent>>();
        var query = EntityQueryEnumerator<ESEntityTimerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var timer, out var xform))
        {
            if (_timing.CurTime < timer.TimerEnd)
                continue;

            firingTimers.Add((uid, timer, xform));
        }

        // loop over firing timers separately to avoid collection exceptions from adding new timers in an event raise
        foreach (var (uid, timer, xform) in firingTimers)
        {
            var target = xform.ParentUid;

            // broadcast
            if (xform.MapID == MapId.Nullspace)
            {
                RaiseLocalEvent((object) timer.TimerEndEvent);
            }
            else if (TimerTargetIsValid(target))
            {
                RaiseLocalEvent(target, (object) timer.TimerEndEvent);
            }

            PredictedQueueDel(uid);
        }
    }
}

[NonNetworkedTimerEvent]
public sealed partial class MethodTimerEvent : ESEntityTimerEvent
{
    public readonly Action Method;

    public MethodTimerEvent(Action method)
    {
        Method = method;
    }
}

/// <summary>
///     Used by integration tests.
/// </summary>
[NetSerializable, Serializable]
public sealed partial class TestTimerEvent : ESEntityTimerEvent;
