using System.Linq;
using Content.Shared.GameTicking;
using Content.Server.Station.Components;
using Robust.Shared.Network;
using Robust.Shared.Player;
using System.Text;
using Content.Server.Spawners.Components;
using Content.Shared.Alert;
using Content.Shared.CCVar;
using Content.Shared.Players;
using Robust.Shared.EntitySerialization;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Server.GameTicking
{
    public sealed partial class GameTicker
    {
        [ViewVariables]
        private readonly Dictionary<NetUserId, PlayerGameStatus> _playerGameStatuses = new();

        [ViewVariables]
        private TimeSpan _roundStartTime;

        /// <summary>
        /// How long before RoundStartTime do we load maps.
        /// </summary>
        [ViewVariables]
        public TimeSpan RoundPreloadTime { get; } = TimeSpan.FromSeconds(15);

        [ViewVariables]
        private TimeSpan _pauseTime;

        [ViewVariables]
        public new bool Paused { get; set; }

        [ViewVariables]
        private bool _roundStartCountdownHasNotStartedYetDueToNoPlayers;

        /// <summary>
        /// The game status of a players user Id. May contain disconnected players
        /// </summary>
        public IReadOnlyDictionary<NetUserId, PlayerGameStatus> PlayerGameStatuses => _playerGameStatuses;

        // ES START
        public MapId? DiegeticLobbyMapId = null;
        // todo mirror lobby change
        private static readonly EntProtoId TheatergoerEntity = "ESMobTheatergoer";

        private static readonly ProtoId<AlertPrototype> ReadiedAlert = "ESReadiedUp";
        private static readonly ProtoId<AlertPrototype> NotReadiedAlert = "ESNotReadiedUp";
        private static readonly ProtoId<AlertPrototype> ObservingAlert = "ESObserving";
        private static readonly ProtoId<AlertCategoryPrototype> ReadyAlertCategory = "ESReadyStatus";

        // Manages loading the diegetic lobby world and spawning players into it.
        // FOR MIRROR NOTES
        // lobby persists thru restarts (?)
        // create once at server start
        // characters also persist
        // diegetic mechanism for readying = chairs diegetic mechanism for marking as observer = uhh idk lol
        // maptext for directions, 'projector' entit ythat shows maptext, use a different font, idk
        private void CreateLobbyWorld()
        {
            if (_runLevel != GameRunLevel.PreRoundLobby)
                return;

            var mapPath = _cfg.GetCVar(CCVars.GameDiegeticLobbyMap);

            _sawmill.Info("Creating diegetic lobby..");
            var opts = DeserializationOptions.Default with {InitializeMaps = true};
            if (!_loader.TryLoadMap(new ResPath(mapPath),
                    out var map,
                    out _,
                    opts))
            {
                throw new Exception($"Failed to load diegetic lobby map {mapPath}");
            }

            DiegeticLobbyMapId = map.Value.Comp.MapId;
            _sawmill.Info($"Created diegetic lobby at map ID {DiegeticLobbyMapId.Value}");

            // invent a guy for everyone in the server
            foreach (var player in _playerManager.Sessions)
            {
                AttachPlayerToLobbyCharacter(player);
            }

            var ev = new ESLobbyWorldCreatedEvent();
            RaiseLocalEvent(ref ev);
        }

        private void AttachPlayerToLobbyCharacter(ICommonSession session)
        {
            if (session.ContentData() is not { } data)
                return;

            if (data.LobbyEntity != null)
            {
                _sawmill.Info($"Attaching {session.Name} to existing lobby character");
                _playerManager.SetAttachedEntity(session, data.LobbyEntity.Value, true);
                return;
            }

            _sawmill.Info($"Creating lobby character for {session.Name}");
            var spawnPosition = GetTheatergoerSpawnPoint();
            var theatergoer = SpawnAtPosition(TheatergoerEntity, spawnPosition);
            _playerManager.SetAttachedEntity(session, theatergoer, true);
            _metaData.SetEntityName(theatergoer, session.Name);
            data.LobbyEntity = theatergoer;
        }

        private EntityCoordinates GetTheatergoerSpawnPoint()
        {
            if (DiegeticLobbyMapId == null)
                return EntityCoordinates.Invalid;

            var possible = new List<EntityCoordinates>();
            var spawnPointQuery = EntityQueryEnumerator<SpawnPointComponent, TransformComponent>();
            while (spawnPointQuery.MoveNext(out var uid, out var point, out var transform))
            {
                if (point.SpawnType != SpawnPointType.Theatergoer
                    || TerminatingOrDeleted(uid)
                    || transform.MapUid == null
                    || TerminatingOrDeleted(transform.MapUid.Value)
                    || transform.MapID != DiegeticLobbyMapId)
                {
                    continue;
                }

                possible.Add(transform.Coordinates);
            }

            if (possible.Count != 0)
                return _robustRandom.Pick(possible);

            _sawmill.Error("Can't find any theatergoer spawn points!");
            return EntityCoordinates.Invalid;
        }

        // called on round restart
        private void CleanupLobbyWorld()
        {
            _sawmill.Info("Cleaning up lobby world");

            // it might be necessary to raise RoundRestartCleanup
            // (for when lobby transitions to gameplay)
            // I really don't want bugs where bad entity systems persist data from the diegetic lobby
            // into the actual game, so it should be fine to trick them a little bit, but also
            // I think it'll introduce more weirdness with stuff that assumes we were actually in game?
            // so for now I'm not doing it
            // ideally, we just have nothing in the diegetic lobby that actually relies on anything that would need
            // to be cleaned up, and nothing subscribing to roundrestartcleanup that doesn't care about roundflow
            // but this is probably a pipe dream.

            foreach (var player in _playerManager.Sessions)
            {
                if (player.ContentData() is not { } data)
                    continue;

                data.LobbyEntity = null;
            }
        }
        // ES END

        public void UpdateInfoText()
        {
            RaiseNetworkEvent(GetInfoMsg(), Filter.Empty().AddPlayers(_playerManager.NetworkedSessions));
        }

        private string GetInfoText()
        {
            var preset = CurrentPreset ?? Preset;
            if (preset == null)
            {
                return string.Empty;
            }

            var playerCount = $"{_playerManager.PlayerCount}";
            var readyCount = _playerGameStatuses.Values.Count(x => x == PlayerGameStatus.ReadyToPlay);

            var stationNames = new StringBuilder();
            var query =
                EntityQueryEnumerator<StationJobsComponent, StationSpawningComponent, MetaDataComponent>();

            var foundOne = false;

            while (query.MoveNext(out _, out _, out var meta))
            {
                foundOne = true;
                if (stationNames.Length > 0)
                    stationNames.Append('\n');

                stationNames.Append(meta.EntityName);
            }

            if (!foundOne)
            {
                stationNames.Append(_gameMapManager.GetSelectedMap()?.MapName ??
                                    Loc.GetString("game-ticker-no-map-selected"));
            }

            var gmTitle = (Decoy == null) ? Loc.GetString(preset.ModeTitle) : Loc.GetString(Decoy.ModeTitle);
            var desc = (Decoy == null) ? Loc.GetString(preset.Description) : Loc.GetString(Decoy.Description);
            return Loc.GetString(
                RunLevel == GameRunLevel.PreRoundLobby
                    ? "game-ticker-get-info-preround-text"
                    : "game-ticker-get-info-text",
                ("roundId", RoundId),
                ("playerCount", playerCount),
                ("readyCount", readyCount),
                ("mapName", stationNames.ToString()),
                ("gmTitle", gmTitle),
                ("desc", desc));
        }

        private TickerConnectionStatusEvent GetConnectionStatusMsg()
        {
            return new TickerConnectionStatusEvent(RoundStartTimeSpan);
        }

        private TickerLobbyStatusEvent GetStatusMsg(ICommonSession session)
        {
            _playerGameStatuses.TryGetValue(session.UserId, out var status);
            return new TickerLobbyStatusEvent(RunLevel != GameRunLevel.PreRoundLobby, LobbyBackground, status, _roundStartTime, RoundPreloadTime, RoundStartTimeSpan, Paused);
        }

        private void SendStatusToAll()
        {
            foreach (var player in _playerManager.Sessions)
            {
                RaiseNetworkEvent(GetStatusMsg(player), player.Channel);
            }
        }

        private TickerLobbyInfoEvent GetInfoMsg()
        {
            return new(GetInfoText());
        }

        private void UpdateLateJoinStatus()
        {
            RaiseNetworkEvent(new TickerLateJoinStatusEvent(DisallowLateJoin));
        }

        public bool PauseStart(bool pause = true)
        {
            if (Paused == pause)
            {
                return false;
            }

            Paused = pause;

            if (pause)
            {
                _pauseTime = _gameTiming.CurTime;
            }
            else if (_pauseTime != default)
            {
                _roundStartTime += _gameTiming.CurTime - _pauseTime;
            }

            RaiseNetworkEvent(new TickerLobbyCountdownEvent(_roundStartTime, Paused));

            _chatManager.DispatchServerAnnouncement(Loc.GetString(Paused
                ? "game-ticker-pause-start"
                : "game-ticker-pause-start-resumed"));

            return true;
        }

        public bool TogglePause()
        {
            PauseStart(!Paused);
            return Paused;
        }

        public void ToggleReadyAll(bool ready)
        {
            var status = ready ? PlayerGameStatus.ReadyToPlay : PlayerGameStatus.NotReadyToPlay;
            foreach (var playerUserId in _playerGameStatuses.Keys)
            {
                _playerGameStatuses[playerUserId] = status;
                if (!_playerManager.TryGetSessionById(playerUserId, out var playerSession))
                    continue;
                RaiseNetworkEvent(GetStatusMsg(playerSession), playerSession.Channel);
            }
        }

        // ES START
        public void ToggleReady(ICommonSession player, PlayerGameStatus ready)
        // ES END
        {
            if (!_playerGameStatuses.ContainsKey(player.UserId))
                return;

            if (!_userDb.IsLoadComplete(player))
                return;

            if (RunLevel != GameRunLevel.PreRoundLobby)
            {
                return;
            }

            _playerGameStatuses[player.UserId] = ready;
            RaiseNetworkEvent(GetStatusMsg(player), player.Channel);
            // update server info to reflect new ready count
            TryShowReadyStatusAlert(player, ready);
            UpdateInfoText();

// ES START
            var ev = new ESOnPlayerReadyToggled(player, ready);
            RaiseLocalEvent(ref ev);
// ES END
        }

        // ES START
        private void TryShowReadyStatusAlert(ICommonSession player, PlayerGameStatus ready)
        {
            if (player.AttachedEntity is null || ready == PlayerGameStatus.JoinedGame)
                return;

            var alert = ready switch
            {
                PlayerGameStatus.NotReadyToPlay => NotReadiedAlert,
                PlayerGameStatus.ReadyToPlay => ReadiedAlert,
                PlayerGameStatus.Observing => ObservingAlert,
                _ => throw new ArgumentOutOfRangeException()
            };

            _alerts.ShowAlert(player.AttachedEntity.Value, alert);
        }

        // TODO clear alerts on theatergoers when evry1 goes ingame
        private void ClearReadyStatusAlert(ICommonSession player)
        {
            if (player.AttachedEntity == null)
                return;

            _alerts.ClearAlertCategory(player.AttachedEntity.Value, ReadyAlertCategory);
        }

        // ES END
        public bool UserHasJoinedGame(ICommonSession session)
            => UserHasJoinedGame(session.UserId);

        public bool UserHasJoinedGame(NetUserId userId)
            => PlayerGameStatuses.TryGetValue(userId, out var status) && status == PlayerGameStatus.JoinedGame;
    }
    // ES START
    [ByRefEvent]
    public readonly record struct ESLobbyWorldCreatedEvent;
    // ES END
}
