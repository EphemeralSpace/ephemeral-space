using System.Numerics;
using Content.Server._ES.Arrivals.Components;
using Content.Server.GameTicking;
using Content.Server.Gravity;
using Content.Server.Shuttles.Components;
using Content.Server.Shuttles.Events;
using Content.Server.Shuttles.Systems;
using Content.Server.Spawners.EntitySystems;
using Content.Server.Station.Events;
using Content.Server.Station.Systems;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Light.Components;
using Content.Shared.Bed.Cryostorage;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Gravity;
using Content.Shared.Movement.Components;
using Content.Shared.Shuttles.Components;
using Content.Shared.Tag;
using Content.Shared.Tiles;
using Robust.Server.GameObjects;
using Robust.Shared.Configuration;
using Robust.Shared.EntitySerialization.Systems;
using Robust.Shared.Map;
using Robust.Shared.Map.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Arrivals;

public sealed class ESArrivalsSystem : EntitySystem
{
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly GravitySystem _gravity = default!;
    [Dependency] private readonly MapSystem _map = default!;
    [Dependency] private readonly MapLoaderSystem _mapLoader = default!;
    [Dependency] private readonly ShuttleSystem _shuttle = default!;
    [Dependency] private readonly StationSystem _station = default!;
    [Dependency] private readonly StationSpawningSystem _stationSpawning = default!;

    private bool _arrivalsEnabled = true;

    private static readonly ProtoId<TagPrototype> DockTagProto = "DockArrivals";

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESStationArrivalsComponent, StationPostInitEvent>(OnStationPostInit);

        SubscribeLocalEvent<ESArrivalsShuttleComponent, FTLTagEvent>(OnShuttleTag);

        SubscribeLocalEvent<PlayerSpawningEvent>(HandlePlayerSpawning, before: [typeof(SpawnPointSystem)]);
        SubscribeLocalEvent<PlayerSpawnCompleteEvent>(OnPlayerSpawnComplete);

        _config.OnValueChanged(ESCVars.ESArrivalsEnabled, OnArrivalsConfigChanged, true);
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

    private void HandlePlayerSpawning(PlayerSpawningEvent ev)
    {
        if (ev.SpawnResult != null)
            return;

        // Always handle spawning, even at roundstart
        if (!_arrivalsEnabled)
            return;

        if (!TryComp<ESStationArrivalsComponent>(ev.Station, out var arrivals) || arrivals.ShuttleUid is not { } grid)
            return;

        // TODO mirror actually place them into the cryostorage
        var points = EntityQueryEnumerator<CryostorageComponent, TransformComponent>();
        var possiblePositions = new List<EntityCoordinates>();
        while (points.MoveNext(out _, out _, out var xform))
        {
            if (xform.GridUid != grid)
                continue;

            possiblePositions.Add(xform.Coordinates);
        }

        var spawnLoc = possiblePositions.Count > 0 ? _random.Pick(possiblePositions) : new EntityCoordinates(grid, Vector2.Zero);
        ev.SpawnResult = _stationSpawning.SpawnPlayerMob(
            spawnLoc,
            ev.Job,
            ev.HumanoidCharacterProfile,
            ev.Station);

        // TODO MIRROR one-way arrivals, use these for a turnstile check or something + remove on exiting arrivals
        // EnsureComp<AutoOrientComponent>(ev.SpawnResult.Value);
        var passenger = EnsureComp<ESArrivalsPassengerComponent>(ev.SpawnResult.Value);
        passenger.Station = ev.Station.Value;
    }

    // TODO MIRROR why does this even exist
    private void OnPlayerSpawnComplete(PlayerSpawnCompleteEvent args)
    {
        // Exists solely as a remedial testing thing for if someone
        // spawns in without arrivals. We don't handle basic roundstart behavior because uhh...
        // Fuck you! That's why. We don't have typical roundflow so go fuck yourself.
        if (_arrivalsEnabled || HasComp<ESArrivalsPassengerComponent>(args.Mob))
            return;

        // Match the one above.
        var ev = new ESPlayersArrivedEvent([args.Mob]);
        RaiseLocalEvent(args.Station, ref ev, true);
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

        var arrivalsComp = EnsureComp<ESArrivalsShuttleComponent>(shuttle.Value);
        arrivalsComp.Station = ent;
        EnsureComp<ProtectedGridComponent>(shuttle.Value);
        EnsureComp<PreventPilotComponent>(shuttle.Value);

        FlyToStation((shuttle.Value, arrivalsComp));

        _map.DeleteMap(mapId);
    }

    private void FlyToStation(Entity<ESArrivalsShuttleComponent> ent)
    {
        if (_station.GetLargestGrid(ent.Comp.Station) is not { } grid)
            return;

        var flightTime = _config.GetCVar(ESCVars.ESArrivalsFTLTime);
        _shuttle.FTLToDock(ent, Comp<ShuttleComponent>(ent), grid, startupTime: 0f, hyperspaceTime: flightTime);
    }
}

[ByRefEvent]
public readonly record struct ESPlayersArrivedEvent(List<EntityUid> Players);
