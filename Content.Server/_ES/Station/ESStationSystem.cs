using System.Linq;
using Content.Server._ES.Station.Components;
using Content.Server.GameTicking;
using Content.Server.Maps;
using Content.Server.Procedural;
using Content.Server.Shuttles.Systems;
using Content.Server.Station.Components;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Light.Components;
using Content.Shared._ES.Station;
using Content.Shared.CCVar;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Enums;
using Robust.Shared.Map;
using Robust.Shared.Physics;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Station;

/// <summary>
///     Handles ES-specific station handling -- technically supports multistation, though we aren't using it initially
///     Better support for dungeons/debris/map components and what not than normal station configs
/// </summary>
public sealed class ESStationSystem : ESSharedStationSystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IMapManager _mapManager = default!;
    [Dependency] private readonly IPlayerManager _playerManager = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IGameMapManager _gameMap = default!;
    [Dependency] private readonly DungeonSystem _dungeon = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly TransformSystem _transform = default!;

    private static readonly ProtoId<ESStationConfigPrototype> DefaultConfig = "ESDefault";

    private bool _enabled;
    private string _currentConfig = DefaultConfig;

    private readonly Dictionary<ProtoId<JobPrototype>, int?> _availableRoundstartJobs = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESLobbyWorldCreatedEvent>(OnLobbyWorldCreated);

        SubscribeLocalEvent<LoadingMapsEvent>(OnLoadingMaps);
        SubscribeLocalEvent<PostGameMapLoad>(OnPostGameMapLoad);
        SubscribeLocalEvent<ESLoadIntoDefaultMapEvent>(OnPostLoadingMaps);

        Subs.CVar(_config, ESCVars.ESStationEnabled, value => _enabled = value, true);
        Subs.CVar(_config, ESCVars.ESStationCurrentConfig, OnStationCurrentConfigChanged, true);

        _config.OnValueChanged(CCVars.GridFill, OnGridFillChanged);

        _playerManager.PlayerStatusChanged += (_, args) =>
        {
            // Only send if they're entering the lobby.
            if (args.NewStatus is SessionStatus.Disconnected)
                return;
            RaiseNetworkEvent(new ESUpdateAvailableRoundstartJobs(_availableRoundstartJobs), args.Session);
        };
    }

    private void OnLobbyWorldCreated(ref ESLobbyWorldCreatedEvent ev)
    {
        RefreshAvailableJobs();
    }

    private void OnGridFillChanged(bool obj)
    {
        if (!obj)
            return;

        var mapQuery = EntityQueryEnumerator<ESStationMapComponent>();
        while (mapQuery.MoveNext(out var uid, out var comp))
        {
            LoadExtraGrids((uid, comp));
        }
    }

    private void OnLoadingMaps(LoadingMapsEvent ev)
    {
        if (!_enabled)
            return;

        // if stationcount is 1 then the map will be loaded by
        // normal mapload logic
        // otherwise we should clear
        var config = GetConfig();

        if (GetStationCount(config) == 1)
            return;

        ev.Maps.Clear();
    }

    private void OnPostGameMapLoad(PostGameMapLoad ev)
    {
        var config = GetConfig();

        foreach (var grid in ev.Grids)
        {
            EntityManager.AddComponents(grid, config.StationGridComponents);
        }
    }

    private void OnPostLoadingMaps(ref ESLoadIntoDefaultMapEvent ev)
    {
        if (!_enabled)
            return;

        var config = GetConfig();
        var stationCount = GetStationCount(config);

        var map = _gameMap.GetSelectedMap();
        if (map == null)
            return;

        var baseAngle = _random.NextAngle();

        // if we have more than 1 station assume these are all grids
        // and load them ourselves
        if (stationCount != 1)
        {
            for (var i = 0; i < stationCount; i++)
            {
                baseAngle += Math.Tau / stationCount;

                if (!_mapLoader.TryLoadGrid(ev.DefaultMapId,
                        map.MapPath,
                        out var grid,
                        DeserializationOptions.Default,
                        baseAngle.ToVec() * config.StationDistance,
                        _random.NextAngle()))
                {
                    throw new Exception($"Failed to load game-map grid {map.ID}");
                }

                var g = new List<EntityUid> { grid.Value.Owner };
                RaiseLocalEvent(new PostGameMapLoad(map, ev.DefaultMapId, g, null));
            }
        }

        // Add map-specific things after loading
        // not before, in case they get overwritten by loading a map
        EntityManager.AddComponents(ev.DefaultMap, config.MapComponents);

        var configComp = EnsureComp<ESStationMapComponent>(ev.DefaultMap);
        configComp.Config = config.ID;

        LoadExtraGrids(ev.DefaultMap);
    }

    private void OnStationCurrentConfigChanged(string value)
    {
        if (_currentConfig == value)
            return;

        _currentConfig = value;
        RefreshAvailableJobs();
    }

    private ESStationConfigPrototype GetConfig()
    {
        if (!_prototype.TryIndex<ESStationConfigPrototype>(_currentConfig, out var config))
            config = _prototype.Index(DefaultConfig);

        return config;
    }

    private int GetStationCount(ESStationConfigPrototype config)
    {
        return Math.Clamp(_playerManager.PlayerCount / config.PlayersPerStation, config.MinStations, config.MaxStations);
    }

    private async void LoadExtraGrids(Entity<ESStationMapComponent?> map)
    {
        if (!_config.GetCVar(CCVars.GridFill))
            return;

        if (!Resolve(map, ref map.Comp) || map.Comp.GridsLoaded)
            return;

        var config = _prototype.Index(map.Comp.Config);

        foreach (var dungeon in config.Dungeons)
        {
            var count = dungeon.Count.Get(_random.GetRandom());
            for (var i = 0; i < count; i++)
            {
                _map.CreateMap(out var mapId);
                var spawnedGrid = _mapManager.CreateGridEntity(mapId);

                EntityManager.AddComponents(spawnedGrid, dungeon.Components);

                var dungeonProto = _prototype.Index(_random.Pick(dungeon.Configs));
                var distance = dungeon.Distance.Get(_random.GetRandom());
                var pos = _random.NextAngle().ToVec() * distance;

                await _dungeon.GenerateDungeonAsync(dungeonProto,
                    spawnedGrid.Owner,
                    spawnedGrid.Comp,
                    Vector2i.Zero,
                    _random.Next());

                var coords = new EntityCoordinates(map, pos);
                if (dungeon.ForcePos)
                {
                    var gridXform = Transform(spawnedGrid);

                    var angle = _random.NextAngle();

                    var transform = new Transform(_transform.ToWorldPosition(gridXform.Coordinates), angle);
                    var adjustedOffset = Robust.Shared.Physics.Transform.Mul(transform, spawnedGrid.Comp.LocalAABB.Center);

                    _transform.SetCoordinates(spawnedGrid, coords.Offset(adjustedOffset));
                }
                else
                {
                    _shuttle.TryFTLProximity(spawnedGrid.Owner, coords);
                }

                if (dungeon.Name is { } name)
                {
                    _meta.SetEntityName(spawnedGrid, Loc.GetString(_random.Pick(_prototype.Index(name).Values)));
                }
            }
        }

        map.Comp.GridsLoaded = true;
    }

    public void RefreshAvailableJobs()
    {
        var config = GetConfig();

        _availableRoundstartJobs.Clear();

        // balls logic because of old mutistation
        var map = _gameMap.GetSelectedMap();
        if (map == null)
            return;

        for (int i = 0; i < GetStationCount(config); i++)
        {
            foreach (var station in map.Stations.Values)
            {
                // Yes, we basically just rely on the jobs to be specified here.
                // Every map does it like this but it's still a very imperfect solution.
                // This also doesn't account for a future system that might modify the jobs
                // randomly on shift start (wildcards, etc.)
                if (!station.StationComponentOverrides.TryGetComponent<StationJobsComponent>(EntityManager.ComponentFactory, out var jobs))
                    continue;

                foreach (var (job, counts) in jobs.SetupAvailableJobs)
                {
                    var count = counts[0];

                    if (count == 0)
                        continue;

                    if (!_availableRoundstartJobs.TryGetValue(job, out var value))
                        value = 0;
                    _availableRoundstartJobs[job] = value + count;
                }
            }
        }

        RaiseNetworkEvent(new ESUpdateAvailableRoundstartJobs(_availableRoundstartJobs));
    }
}

/// <summary>
/// BALLS
/// </summary>
[ByRefEvent]
public readonly record struct ESLoadIntoDefaultMapEvent(MapId DefaultMapId, EntityUid DefaultMap)
{
    public readonly MapId DefaultMapId = DefaultMapId;
    public readonly EntityUid DefaultMap = DefaultMap;
}
