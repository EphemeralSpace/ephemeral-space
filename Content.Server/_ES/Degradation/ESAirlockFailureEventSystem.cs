using Content.Server._ES.Degradation.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Degradation;
using Content.Shared.GameTicking.Components;
using Content.Shared.Humanoid;
using Robust.Shared.Player;
using Robust.Shared.Random;

namespace Content.Server._ES.Degradation;

public sealed class ESAirlockFailureEventSystem : StationEventSystem<ESAirlockFailureEventComponent>
{
    [Dependency] private readonly ESAirlockFailureSystem _airlockFailure = default!;

    protected override void Started(EntityUid uid,
        ESAirlockFailureEventComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        var validTargets = new List<EntityUid>();

        var players = EntityQueryEnumerator<ActorComponent, HumanoidAppearanceComponent>();
        while (players.MoveNext(out var player, out _, out _))
        {
            validTargets.Add(player);
        }

        foreach (var player in RobustRandom.GetItems(validTargets, RobustRandom.Next(component.MinCount, component.MaxCount + 1)))
        {
            _airlockFailure.AddFailureCharge(player);
        }

        ForceEndSelf(uid, gameRule);
        QueueDel(uid);
    }
}
