using System.Linq;
using Content.Server._ES.Arrivals.Components;
using Content.Server.DeviceNetwork.Systems;
using Content.Server.GameTicking;
using Content.Server.Screens.Components;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Light.Components;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.DeviceNetwork;
using Content.Shared.DeviceNetwork.Components;
using Content.Shared.Gravity;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;
using Content.Shared.Shuttles.Components;
using Content.Shared.StatusEffectNew;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Server.Containers;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.Containers;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Timing;

namespace Content.Server._ES.Arrivals;

public sealed class ESArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ContainerSystem _container = default!;
    [Dependency] private readonly DeviceNetworkSystem _deviceNetwork = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly HungerSystem _hunger = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;
    [Dependency] private readonly StatusEffectsSystem _statusEffects = default!;
    [Dependency] private readonly ThirstSystem _thirst = default!;

    private bool _arrivalsEnabled = true;
    private float _flightTime;

    private static readonly ProtoId<TagPrototype> DockTagProto = "DockArrivals";
    private static readonly EntProtoId SleepStatusEffect = "StatusEffectForcedSleeping";
    private static readonly EntProtoId CryoSicknessEffect = "ESStatusEffectCryoSickness";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESStationArrivalsComponent, StationPostInitEvent>(OnStationPostInit);

        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLTagEvent>(OnShuttleTag);
        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLStartedEvent>(OnFTLStarted);
        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLCompletedEvent>(OnFTLCompleted);

        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: [typeof(SpawnPointSystem)]);

        _config.OnValueChanged(ESCVars.ESArrivalsEnabled, OnArrivalsConfigChanged, true);
        _config.OnValueChanged(ESCVars.ESArrivalsFTLTime, (f => _flightTime = f), true);
    }

    private void OnArrivalsConfigChanged(bool val)
    {
        if (_arrivalsEnabled && !val && _gameTicker.RunLevel != GameRunLevel.PreRoundLobby)
        {
            Log.Error("EmoGarbage didn't bother implementing disabling arrivals mid-round.");
            return;
        }

        _arrivalsEnabled = val;

        if (_arrivalsEnabled)
        {
            var query = EntityQueryEnumerator<ESStationArrivalsComponent>();
            while (query.MoveNext(out var uid, out var comp))
            {
                SetupShuttle((uid, comp));
            }
        }
    }

    private void OnStationPostInit(Entity<ESStationArrivalsComponent> ent, ref StationPostInitEvent args)
    {
        if (!_arrivalsEnabled)
            return;

        SetupShuttle(ent);
    }

    private void OnShuttleTag(Entity<ESArrivalsShuttleComponent> ent, ref FTLTagEvent args)
    {
        if (args.Handled)
            return;

        args.Handled = true;
        args.Tag = DockTagProto;
    }

    private void OnFTLStarted(Entity<ESArrivalsShuttleComponent> ent, ref FTLStartedEvent args)
    {
        if (!TryComp<DeviceNetworkComponent>(ent, out var netComp))
            return;

        var payload = new NetworkPayload
        {
            [ShuttleTimerMasks.ShuttleMap] = Transform(ent).MapUid,
            [ShuttleTimerMasks.SourceMap] = args.FromMapUid,
            [ShuttleTimerMasks.ShuttleTime] = TimeSpan.FromSeconds(_flightTime),
            [ShuttleTimerMasks.SourceTime] = TimeSpan.FromSeconds(_flightTime),
        };

        _deviceNetwork.QueuePacket(ent, null, payload, netComp.TransmitFrequency);
    }

    private void HandlePlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        // Always handle spawning, even at roundstart
        if (!_arrivalsEnabled)
            return;

        if (!TryComp<ESStationArrivalsComponent>(ev.Station, out var arrivals) || arrivals.ShuttleUid is not { } grid)
            return;

        var points = EntityQueryEnumerator<CryostorageComponent, TransformComponent>();
        var possiblePositions = new List<(EntityUid, BaseContainer)>();
        while (points.MoveNext(out var uid, out var cryo, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            if (!_container.TryGetContainer(uid, cryo.ContainerId, out var container) || container.ContainedEntities.Any())
                continue;

            possiblePositions.Add((uid, container));
        }

        var (cryostorage, cryoContainer) = possiblePositions.Count > 0 ? _random.Pick(possiblePositions) : (EntityUid.Invalid, default);
        var spawnLoc = cryostorage.Valid ? Transform(cryostorage).Coordinates : new EntityCoordinates(grid, 0, 0);

        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            ev.Job,
            ev.HumanoidCharacterProfile,
            ev.Station);

        if (cryostorage.Valid)
            _container.Insert(ev.SpawnResult.Value, cryoContainer!);

        if (TryComp<HungerComponent>(ev.SpawnResult, out var hunger) &&
            hunger.Thresholds.TryGetValue(HungerThreshold.Starving, out var starving))
        {
            _hunger.SetHunger(ev.SpawnResult.Value, starving + _random.NextFloat(-20, 0), hunger);
        }

        if (TryComp<ThirstComponent>(ev.SpawnResult, out var thirst) &&
            thirst.ThirstThresholds.TryGetValue(ThirstThreshold.Parched, out var lowerThirst))
        {
            _thirst.SetThirst(ev.SpawnResult.Value, thirst, lowerThirst + _random.NextFloat(-50, 0));
        }

        _statusEffects.TryAddStatusEffectDuration(ev.SpawnResult.Value, SleepStatusEffect, TimeSpan.FromSeconds(_random.Next(10, 30)));

        if (_timing.CurTime < arrivals.ArrivalTime)
        {
            var sicknessTime = TimeSpan.FromSeconds(Math.Max((arrivals.ArrivalTime - _timing.CurTime).TotalSeconds + _random.Next(0, 10), _random.Next(10, 15)));
            _statusEffects.TryAddStatusEffectDuration(ev.SpawnResult.Value, CryoSicknessEffect, sicknessTime);
        }

        // TODO MIRROR one-way arrivals, use these for a turnstile check or something + remove on exiting arrivals
        // EnsureComp<AutoOrientComponent>(ev.SpawnResult.Value);
        var passenger = EnsureComp<ESArrivalsPassengerComponent>(ev.SpawnResult.Value);
        passenger.Station = ev.Station.Value;
    }

    private void OnFTLCompleted(Entity<ESArrivalsShuttleComponent> ent, ref FTLCompletedEvent args)
    {
        _gameTicker.AnnounceRound();
    }

    private void SetupShuttle(Entity<ESStationArrivalsComponent> ent)
    {
        if (ent.Comp.ShuttleUid is not null)
            return;

        _map.CreateMap(out var mapId);

        if (!_mapLoader.TryLoadGrid(mapId, ent.Comp.ShuttlePath, out var shuttle))
            return;

        // Set up arrivals grid
        EnsureComp<ESTileBasedRoofComponent>(shuttle.Value.Owner);
        var grav = EnsureComp<GravityComponent>(shuttle.Value.Owner);
        grav.Enabled = true;
        grav.Inherent = true;

        // Set up ftl map
        var ftlMap = _shuttle.EnsureFTLMap();
        var mapLight = EnsureComp<MapLightComponent>(ftlMap);
        mapLight.AmbientLightColor = Color.White;

        _shuttle.TryFTLProximity(shuttle.Value, ftlMap);

        ent.Comp.ShuttleUid = shuttle.Value;
        ent.Comp.ArrivalTime = _timing.CurTime + TimeSpan.FromSeconds(_flightTime);

        var arrivalsComp = EnsureComp<ESArrivalsShuttleComponent>(shuttle.Value);
        arrivalsComp.Station = ent;
        EnsureComp<ProtectedGridComponent>(shuttle.Value);
        EnsureComp<PreventPilotComponent>(shuttle.Value);

        FlyToStation((shuttle.Value, arrivalsComp));

        _station.AddGridToStation(ent, shuttle.Value);

        _map.DeleteMap(mapId);
    }

    private void FlyToStation(Entity<ESArrivalsShuttleComponent> ent)
    {
        if (_station.GetLargestGrid(ent.Comp.Station) is not { } grid)
            return;

        _shuttle.FTLToDock(ent, Comp<ShuttleComponent>(ent), grid, startupTime: 0f, hyperspaceTime: _flightTime);
    }
}
