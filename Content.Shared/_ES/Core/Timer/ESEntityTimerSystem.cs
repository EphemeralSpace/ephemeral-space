using Content.Shared._ES.Core.Timer.Components;
using JetBrains.Annotations;
using Robust.Shared.Map;
using Robust.Shared.Timing;

namespace Content.Shared._ES.Core.Timer;

/// <summary>
/// Used for creating generic timers which serialize to the world.
/// </summary>
public sealed class ESEntityTimerSystem : EntitySystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <summary>
    /// Spawns a timer entity that raises a broadcast event after a specified duration.
    /// </summary>
    /// <param name="duration">Duration of the timer</param>
    /// <param name="endEvent">Event that will be raised when the timer is finished</param>
    /// <returns>The timer that was created</returns>
    [PublicAPI]
    public Entity<ESEntityTimerComponent>? SpawnTimer(TimeSpan duration, ESEntityTimerEvent endEvent)
    {
        var uid = Spawn(null, MapCoordinates.Nullspace);
        var comp = AddComp<ESEntityTimerComponent>(uid);

        comp.TimerEndEvent = endEvent;
        comp.TimerEnd = _timing.CurTime + duration;
        Dirty(uid, comp);

        return (uid, comp);
    }

    /// <summary>
    /// Spawns a timer entity that raises a directed event on a target after a specified duration.
    /// </summary>
    /// <param name="target">Entity the event will raise on</param>
    /// <param name="duration">Duration of the timer</param>
    /// <param name="endEvent">Event that will be raised when the timer is finished</param>
    /// <returns>The timer that was created</returns>
    [PublicAPI]
    public Entity<ESEntityTimerComponent>? SpawnTimer(EntityUid target, TimeSpan duration, ESEntityTimerEvent endEvent)
    {
        if (!TimerTargetIsValid(target))
            return null;

        var uid = Spawn();
        var comp = AddComp<ESEntityTimerComponent>(uid);

        _transform.SetParent(uid, target);

        comp.TimerEndEvent = endEvent;
        comp.TimerEnd = _timing.CurTime + duration;
        Dirty(uid, comp);

        return (uid, comp);
    }

    private bool TimerTargetIsValid(EntityUid uid)
    {
        return !TerminatingOrDeleted(uid) && LifeStage(uid) == EntityLifeStage.MapInitialized;
    }

    public override void Update(float frameTime)
    {
        base.Update(frameTime);

        var query = EntityQueryEnumerator<ESEntityTimerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var timer, out var xform))
        {
            if (_timing.CurTime < timer.TimerEnd)
                continue;

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
