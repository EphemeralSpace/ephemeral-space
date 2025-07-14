using Content.Server._ES.Multistation.Components;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Shared._ES.CCVar;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Multistation;

/// <summary>
/// This handles spawning in multiple stations in a round
/// </summary>
public sealed class ESMultistationSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;

    private static readonly ProtoId<ESMultistationConfigPrototype> DefaultConfig = "ESDefault";

    private bool _enabled;
    private string _currentConfig = DefaultConfig;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<LoadingMapsEvent>(OnLoadingMaps);
        SubscribeLocalEvent<ESPostLoadingMapsEvent>(OnPostLoadingMaps);

        Subs.CVar(_config, ESCVars.ESMultistationEnabled, value => _enabled = value, true);
        Subs.CVar(_config, ESCVars.ESMultistationCurrentConfig, value => _currentConfig = value, true);
    }

    private void OnLoadingMaps(LoadingMapsEvent ev)
    {
        if (!_enabled)
            return;

        ev.Maps.Clear();
    }

    private void OnPostLoadingMaps(ref ESPostLoadingMapsEvent ev)
    {
        if (!_enabled)
            return;

        if (!_prototype.TryIndex<ESMultistationConfigPrototype>(_currentConfig, out var config))
            config = _prototype.Index(DefaultConfig);

        var configComp = EnsureComp<ESMultistationMapComponent>(ev.DefaultMap);
        configComp.Config = config.ID;

        var stationCount = Math.Max(_playerManager.PlayerCount / config.PlayersPerStation, config.MinStations);

        var stations = new List<ProtoId<GameMapPrototype>>(stationCount);
        for (var i = 0; i < stationCount; i++)
        {
            stations.Add(_random.Pick(config.MapPool));
        }

        var baseAngle = _random.NextAngle();
        for (var i = 0; i < stationCount; i++)
        {
            baseAngle += Math.Tau / stationCount;

            var station = _prototype.Index(_random.PickAndTake(stations));
            if (!_mapLoader.TryLoadGrid(ev.DefaultMapId,
                    station.MapPath,
                    out var grid,
                    DeserializationOptions.Default,
                    baseAngle.ToVec() * config.StationDistance,
                    _random.NextAngle()))
            {
                throw new Exception($"Failed to load game-map grid {station.ID}");
            }

            var g = new List<EntityUid> { grid.Value.Owner };
            RaiseLocalEvent(new PostGameMapLoad(station, ev.DefaultMapId, g, null));
        }
    }
}

[ByRefEvent]
public readonly record struct ESPostLoadingMapsEvent(MapId DefaultMapId, EntityUid DefaultMap)
{
    public readonly MapId DefaultMapId = DefaultMapId;
    public readonly EntityUid DefaultMap = DefaultMap;
}
