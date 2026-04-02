using System.Linq;
using Content.Server._ES.StationEvents.ElectricalOverload.Components;
using Content.Server.Light.EntitySystems;
using Content.Server.Power.Components;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.TileFires;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.GameTicking.Components;
using Content.Shared.Light.Components;
using Content.Shared.Localizations;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._ES.StationEvents.ElectricalOverload;

public sealed class ESElectricalOverloadRule : StationEventSystem<ESElectricalOverloadRuleComponent>
{
    [Dependency] private readonly EntityLookupSystem _entityLookup = default!;
    [Dependency] private readonly PoweredLightSystem _poweredLight = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;
    [Dependency] private readonly ESSharedTileFireSystem _tileFire = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESElectricalOverloadRuleComponent, ESSynchronizedVotesCompletedEvent>(OnSynchronizedVotesCompleted);
        SubscribeLocalEvent<ESApcVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
    }

    private void OnSynchronizedVotesCompleted(Entity<ESElectricalOverloadRuleComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        for (var i = 0; i < args.Results.Count; ++i)
        {
            if (args.TryGetResult<ESEntityVoteOption>(i, out var result) &&
                TryGetEntity(result.Entity, out var apc))
            {
                ent.Comp.Apcs.Add(apc.Value);
            }
        }

        if (TryComp<StationEventComponent>(ent, out var station))
        {
            var location = ContentLocalizationManager.FormatList(ent.Comp.Apcs.Select(a => Name(a)).ToList());
            station.StartAnnouncement = Loc.GetString("es-station-event-electrical-overload-start-announcement",
                ("location", location));
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
        ESElectricalOverloadRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        foreach (var apc in component.Apcs)
        {
            if (TerminatingOrDeleted(apc))
                return;

            var coords = Transform(apc).Coordinates;
            var worldPos = _transform.ToWorldPosition(coords);

            if (_transform.GetGrid(coords) is not { } grid ||
                !TryComp<MapGridComponent>(grid, out var gridComp))
                return;

            // Do fires around the APC itself
            var tiles = _map.GetTilesIntersecting(grid,
                gridComp,
                new Circle(worldPos, component.FireRadius));

            foreach (var tile in tiles)
            {
                var coord = _map.ToCoordinates(tile, gridComp);

                if (RobustRandom.Prob(component.FireChance))
                    _tileFire.TryDoTileFire(coord, stage: RobustRandom.Next(1, 3));
            }

            // Break lights around the APC
            foreach (var light in _entityLookup.GetEntitiesInRange<PoweredLightComponent>(coords, component.Radius))
            {
                _poweredLight.TryDestroyBulb(light);
            }
        }
    }
}
