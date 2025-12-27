using Content.Server._ES.SpawnRegion;
using Content.Server._ES.Voting.Components;
using Content.Server.Pinpointer;
using Content.Server.Station.Systems;
using Content.Shared._ES.Voting.Components;
using Content.Shared._ES.Voting.Results;
using Robust.Shared.Utility;

namespace Content.Server._ES.Voting;

public sealed class ESRandomLocationVoteSystem : EntitySystem
{
    [Dependency] private readonly NavMapSystem _navMap = default!;
    [Dependency] private readonly ESSpawnRegionSystem _spawnRegion = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly SharedTransformSystem _transform = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESRandomLocationVoteComponent, ESGetVoteOptionsEvent>(OnGetVoteOptions);
    }

    private void OnGetVoteOptions(Entity<ESRandomLocationVoteComponent> ent, ref ESGetVoteOptionsEvent args)
    {
        if (_station.GetStationsSet().FirstOrNull() is not { } station)
            return;

        for (var i = 0; i < ent.Comp.Count; i++)
        {
            if (!_spawnRegion.TryGetRandomCoords(station, out var coords, checkPlayerLOS: ent.Comp.CheckLOS))
                break;
            var mapCoords = _transform.ToMapCoordinates(coords.Value);

            args.Options.Add(new ESEntityCoordinateVoteOption
            {
                Coordinates = GetNetCoordinates(coords.Value),
                DisplayString = FormattedMessage.RemoveMarkupPermissive(_navMap.GetNearestBeaconString(mapCoords)),
            });
        }
    }
}
