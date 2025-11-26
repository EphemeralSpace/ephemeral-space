using System.Linq;
using Content.Server.Administration.Managers;
using Content.Server.GameTicking;
using Content.Server.Station.Components;
using Content.Server.Station.Systems;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._ES.Spawning;
using Content.Shared.CCVar;
using Content.Shared.GameTicking;
using Content.Shared.Ghost;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Serialization;

namespace Content.Server._ES.Spawning;

/// <inheritdoc/>
public sealed class ESSpawningSystem : ESSharedSpawningSystem
{
    [Dependency] private readonly IAdminManager _adminManager = default!;
    [Dependency] private readonly IConfigurationManager _config = default!;
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly ESEntityTimerSystem _timer = default!;
    [Dependency] private readonly GameTicker _gameTicker = default!;
    [Dependency] private readonly StationJobsSystem _stationJobs = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeNetworkEvent<ESSpawnPlayerEvent>(OnSpawnPlayer);
        SubscribeLocalEvent<ESSpawnPlayerAfterCurtainsEvent>(OnAfterCurtains);
    }

    private void OnSpawnPlayer(ESSpawnPlayerEvent msg, EntitySessionEventArgs args)
    {
        if (_gameTicker.RunLevel == GameRunLevel.PreRoundLobby)
            return;

        _timer.SpawnTimer(TimeSpan.FromSeconds(1.5f), new ESSpawnPlayerAfterCurtainsEvent(msg, args));
    }

    private void OnAfterCurtains(ESSpawnPlayerAfterCurtainsEvent ev)
    {
        var args = ev.Args;
        var msg = ev.Msg;
        var player = args.SenderSession;

        // could have disconnected in the time it took to fire
        if (player.Status != SessionStatus.InGame)
            return;

        // TODO: communicate this in the ui
        if (_gameTicker.JoinedPlayers.Contains(args.SenderSession.UserId))
            return;

        var jobPrototype = _prototype.Index(msg.JobId);

        var selectedStations = msg.Stations.Select(GetEntity);
        var potentialStations = new List<(EntityUid, int)>();
        foreach (var uid in selectedStations)
        {
            if (!TryComp<StationJobsComponent>(uid, out var jobs))
                continue;

            if (!_stationJobs.TryGetJobSlot(uid, jobPrototype,  out var slots, jobs) || slots == 0)
                continue;

            potentialStations.Add((uid, jobs.PlayerJobs.Values.Sum(p => p.Count)));
        }

        if (potentialStations.Count == 0)
        {
            // No job available. Probably due to latency or smth
            return;
        }

        // We favor sending people to less-populated stations.
        // This still lets stations get unbalanced from intentional joining, but uh...
        // I don't give a fuck.
        _random.Shuffle(potentialStations);
        var station = potentialStations.MinBy(p => p.Item2).Item1;

        // in game, check reqs
        if (_gameTicker.PlayerGameStatuses.TryGetValue(args.SenderSession.UserId, out var status) && status == PlayerGameStatus.JoinedGame)
        {
            if (!RespawnsEnabled)
                return;

            // don't allow respawning as a non-ghost
            if (!HasComp<GhostComponent>(args.SenderSession.AttachedEntity))
                return;

            if (GetRespawnTime(args.SenderSession) > Timing.CurTime)
                return;
        }

        if (_gameTicker.RunLevel == GameRunLevel.InRound)
        {
            if (!_stationJobs.TryGetJobSlot(station, jobPrototype, out var slots) || slots == 0)
                return;

            if (_adminManager.IsAdmin(player) && _config.GetCVar(CCVars.AdminDeadminOnJoin))
            {
                _adminManager.DeAdmin(player);
            }

            _gameTicker.MakeJoinGame(args.SenderSession, station, msg.JobId);
            return;
        }

        _gameTicker.MakeJoinGame(args.SenderSession, EntityUid.Invalid);
    }
}

public sealed partial class ESSpawnPlayerAfterCurtainsEvent : ESEntityTimerEvent
{
    public ESSpawnPlayerEvent Msg;
    public EntitySessionEventArgs Args;

    public ESSpawnPlayerAfterCurtainsEvent(ESSpawnPlayerEvent msg, EntitySessionEventArgs args)
    {
        Msg = msg;
        Args = args;
    }
}
