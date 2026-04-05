using Content.Server._ES.Degradation.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.StationEvents.Events;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Station.Components;
using Robust.Shared.Random;

namespace Content.Server._ES.Degradation;

public sealed class ESLightFailureEventSystem : StationEventSystem<ESLightFailureEventComponent>
{
    [Dependency] private readonly PoweredLightSystem _lights = default!;

    protected override void Started(EntityUid uid,
        ESLightFailureEventComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (!TryGetRandomStation(out var chosenStation))
            return;

        var validTargets = new List<Entity<PoweredLightComponent>>();

        var lights = EntityQueryEnumerator<PoweredLightComponent, TransformComponent>();
        while (lights.MoveNext(out var lightUid, out var light, out var xform))
        {
            // this is mildly arbitrary but i mostly want this to be breaking tubes rather than bulbs
            // to get across the idea of shit slowly decaying. bulbs are mostly like small maint rooms and stuff
            // rather than halls or dept rooms where its more noticeable of an effect
            if (!light.CurrentLit || light.BulbType == LightBulbType.Bulb)
                continue;

            if (CompOrNull<StationMemberComponent>(xform.GridUid)?.Station != chosenStation)
                continue;

            validTargets.Add((lightUid, light));
        }

        foreach (var (lightUid, light) in RobustRandom.GetItems(validTargets, RobustRandom.Next(component.MinCount, component.MaxCount + 1)))
        {
            _lights.TryDestroyBulb(lightUid, light, tileFireChanceOverride: component.TileFireChanceOverride);
        }

        ForceEndSelf(uid, gameRule);
        QueueDel(uid);
    }
}
