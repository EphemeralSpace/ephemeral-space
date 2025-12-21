using Content.Server._ES.StationEvents.ElectricalFire.Components;
using Content.Server._ES.TileFires;
using Content.Server.StationEvents.Components;
using Content.Server.StationEvents.Events;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Content.Shared.GameTicking.Components;
using Robust.Shared.Map.Components;
using Robust.Shared.Random;

namespace Content.Server._ES.StationEvents.ElectricalFire;

public sealed class ESElectricalFireRule : StationEventSystem<ESElectricalFireRuleComponent>
{
    [Dependency] private readonly ESTileFireSystem _tileFire = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;
    [Dependency] private readonly SharedMapSystem _map = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESElectricalFireRuleComponent, ESSynchronizedVotesCompletedEvent>(OnSynchronizedVotesCompleted);
    }

    private void OnSynchronizedVotesCompleted(Entity<ESElectricalFireRuleComponent> ent, ref ESSynchronizedVotesCompletedEvent args)
    {
        if (!args.TryGetResult<ESEntityCoordinateVoteOption>(0, out var coordsOption))
            return;

        var coords = GetCoordinates(coordsOption.Coordinates);
        ent.Comp.TargetCoordinates = coords;

        if (TryComp<StationEventComponent>(ent, out var station))
        {
            station.StartAnnouncement = Loc.GetString("es-station-event-electrical-fire-start-announcement",
                ("location", coordsOption.DisplayString));
        }
    }

    protected override void Started(EntityUid uid,
        ESElectricalFireRuleComponent component,
        GameRuleComponent gameRule,
        GameRuleStartedEvent args)
    {
        base.Started(uid, component, gameRule, args);

        if (component.TargetCoordinates is not { } coords)
            return;

        if (_transform.GetGrid(coords) is not { } grid ||
            !TryComp<MapGridComponent>(grid, out var gridComp))
            return;

        var worldPos = _transform.ToWorldPosition(coords);

        var tiles = _map.GetTilesIntersecting(grid,
            gridComp,
            new Circle(worldPos, component.FireRadius));

        foreach (var tile in tiles)
        {
            var coord = _map.ToCoordinates(tile, gridComp);

            if (RobustRandom.Prob(component.FireChance))
                _tileFire.TryDoTileFire(coord, stage: RobustRandom.Next(1, 3));
        }
    }
}
