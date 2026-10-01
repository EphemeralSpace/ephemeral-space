using Content.Shared._ES.Degradation.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Core.Timer;
using Content.Shared.GameTicking.Components;
using Robust.Server.GameStates;

namespace Content.Server._ES.Degradation;

public sealed partial class ESSpaceTurbulenceEventSystem : StationEventSystem<ESSpaceTurbulenceEventComponent>
{
    [Dependency] private ESEntityTimerSystem _entityTimer = default!;
    [Dependency] private PvsOverrideSystem _pvsOverride = default!;

    protected override void Added(EntityUid uid,
        ESSpaceTurbulenceEventComponent component,
        GameRuleComponent gameRule,
        GameRuleAddedEvent args)
    {
        base.Added(uid, component, gameRule, args);

        _pvsOverride.AddGlobalOverride(uid);
    }

    protected override void Started(EntityUid uid,
        ESSpaceTurbulenceEventComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var jerks = component.JerkRange.Next(RobustRandom);
        var accumulatedDelay = TimeSpan.Zero;

        for (var i = 0; i < jerks; ++i)
        {
            accumulatedDelay += RobustRandom.Next(component.MinJerkDelay, component.MaxJerkDelay);
            _entityTimer.SpawnTimer(uid, accumulatedDelay, new ESSpaceTurbulenceJerkTimerEvent());
        }
    }
}
