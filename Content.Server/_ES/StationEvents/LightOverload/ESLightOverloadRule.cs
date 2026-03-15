using Content.Server._ES.StationEvents.LightOverload.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Robust.Shared.Random;

namespace Content.Server._ES.StationEvents.LightOverload;

public sealed class ESLightOverloadRule : StationEventSystem<ESLightOverloadRuleComponent>
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESLightOverloadRuleComponent, ESSynchronizedVotesCompletedEvent>(OnSynchronizedVotesCompleted);
        SubscribeLocalEvent<ESApcVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
    }

    private void OnSynchronizedVotesCompleted(Entity<ESLightOverloadRuleComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        for (var i = 0; i < args.Results.Count; ++i)
        {
            if (args.TryGetResult<ESEntityVoteOption>(i, out var result) &&
                TryGetEntity(result.Entity, out var apc))
            {
                ent.Comp.Apcs.Add(apc.Value);
            }
        }
    }

    private void OnGetVoteOptions(Entity<ESApcVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        var apcs = new List<EntityUid>();
        var query = EntityQueryEnumerator<ApcComponent>();
        while (query.MoveNext(out var uid, out _))
        {
            apcs.Add(uid);
        }

        foreach (var apc in RobustRandom.GetItems(apcs, Math.Min(apcs.Count, ent.Comp.Count)))
        {
            args.Options.Add(new ESEntityVoteOption
            {
                DisplayString = Name(apc),
                Entity = GetNetEntity(apc),
            });
        }
    }

    protected override void Started(EntityUid uid,
        ESLightOverloadRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        foreach (var apc in component.Apcs)
        {
            if (TerminatingOrDeleted(apc))
                return;


            var coords = Transform(apc).Coordinates;
            foreach (var light in _entityLookup.GetEntitiesInRange<PoweredLightComponent>(coords, component.Radius))
            {
                _poweredLight.TryDestroyBulb(light);
            }
        }
    }
}
