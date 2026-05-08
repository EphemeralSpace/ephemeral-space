using Content.Server._ES.Degradation.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Degradation;
using Content.Shared._ES.Degradation.Components;
using Content.Shared.GameTicking.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Random;

namespace Content.Server._ES.Degradation;

/// <summary>
/// Handles <see cref="ESDegradationEventComponent"/>
/// </summary>
public sealed partial class ESDegradationEventSystem : StationEventSystem<ESDegradationEventComponent>
{
    [Dependency] private ESDegradationSystem _degradation = default!;
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    protected override void Started(EntityUid uid,
        ESDegradationEventComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!EntityManager.ComponentFactory.TryGetRegistration(component.Component, out var registration))
        {
            Log.Error($"Failed to find registration for component with name {component.Component}");
            return;
        }

        var entities = new List<EntityUid>();
        foreach (var (entity, _) in EntityManager.GetAllComponents(registration.Type))
        {
            if (!_entityWhitelist.IsWhitelistPassOrNull(component.Whitelist, entity) ||
                _entityWhitelist.IsWhitelistPass(component.Blacklist, entity))
                continue;

            entities.Add(entity);
        }

        var count = Math.Min(component.Count.Get(RobustRandom.GetRandom()), entities.Count);
        for (var i = 0; i < count; i++)
        {
            var ent = RobustRandom.PickAndTake(entities);

            if (component.DegradeImmediately)
            {
                _degradation.Degrade(ent, null);
            }
            else
            {
                EnsureComp<ESQueuedDegradationComponent>(ent);
            }
        }

        ForceEndSelf(uid, gameRule);
        QueueDel(uid); // We don't need these afterward
    }
}
