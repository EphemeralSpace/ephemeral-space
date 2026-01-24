using Content.Shared.Administration;
using Content.Shared.CCVar;
using Content.Shared.Database;
using Content.Shared.GameTicking;
using Content.Shared.GameWindow;
using Content.Shared.Players;
using Content.Shared.Preferences;
using JetBrains.Annotations;
using Robust.Server.Player;
using Robust.Shared.Audio;
using Robust.Shared.Enums;
using Robust.Shared.Player;
using Robust.Shared.Timing;
using Robust.Shared.Utility;
// ES START
using Content.Server._ES.Stagehand;
using Content.Shared.Mobs.Systems;
// ES END

namespace Content.Server.GameTicking
{
    [UsedImplicitly]
    public sealed partial class GameTicker
    {
        [Dependency] private readonly IPlayerManager _playerManager = default!;
// ES START
        [Dependency] private readonly MobStateSystem _mobState = default!;
        [Dependency] private readonly ESStagehandSystem _stagehand = default!;
// ES END

        private void InitializePlayer()
        {
            _playerManager.PlayerStatusChanged += PlayerStatusChanged;
        }

        private async void PlayerStatusChanged(object? sender, SessionStatusEventArgs args)
        {
            var session = args.Session;

            if (_mind.TryGetMind(session.UserId, out var mindId, out var mind))
            {
                if (args.NewStatus != SessionStatus.Disconnected)
                {
                    _pvsOverride.AddSessionOverride(mindId.Value, session);
                }
            }

            DebugTools.Assert(session.GetMind() == mindId);

            switch (args.NewStatus)
            {
                case SessionStatus.Connected:
                {
                    AddPlayerToDb(args.Session.UserId.UserId);

                    // Always make sure the client has player data.
                    if (session.Data.ContentDataUncast == null)
                    {
                        var data = new ContentPlayerData(session.UserId, args.Session.Name);
                        data.Mind = mindId;
                        session.Data.ContentDataUncast = data;
                    }

                    // Make the player actually join the game.
                    // timer time must be > tick length
                    Timer.Spawn(0, () => _playerManager.JoinGame(args.Session));

                    var record = await _db.GetPlayerRecordByUserId(args.Session.UserId);
                    var firstConnection = record != null &&
                                          Math.Abs((record.FirstSeenTime - record.LastSeenTime).TotalMinutes) < 1;

                    _chatManager.SendAdminAnnouncement(firstConnection
                        ? Loc.GetString("player-first-join-message", ("name", args.Session.Name))
                        : Loc.GetString("player-join-message", ("name", args.Session.Name)));

                    RaiseNetworkEvent(GetConnectionStatusMsg(), session.Channel);

                    if (firstConnection && _cfg.GetCVar(CCVars.AdminNewPlayerJoinSound))
                        _audio.PlayGlobal(new SoundPathSpecifier("/Audio/Effects/newplayerping.ogg"),
                            Filter.Empty().AddPlayers(_adminManager.ActiveAdmins), false,
                            audioParams: new AudioParams { Volume = -5f });

                    if (LobbyEnabled && _roundStartCountdownHasNotStartedYetDueToNoPlayers)
                    {
                        _roundStartCountdownHasNotStartedYetDueToNoPlayers = false;
                        _roundStartTime = _gameTiming.CurTime + LobbyDuration;
                    }

                    break;
                }

                case SessionStatus.InGame:
                {
                    _userDb.ClientConnected(session);

                    if (mind == null)
                    {
                        if (LobbyEnabled)
                        {
                            PlayerJoinLobby(session, true);
                        }
                        else
                            SpawnWaitDb();

                        _adminLogger.Add(LogType.Connection, LogImpact.Low, $"User {args.Session:Player} attached to {(args.Session.AttachedEntity != null ? ToPrettyString(args.Session.AttachedEntity) : "nothing"):entity} connected to the game.");
                        break;
                    }

// ES START
                    if (mind.CurrentEntity == null || Deleted(mind.CurrentEntity) || _mobState.IsDead(mind.CurrentEntity.Value))
                    {
                        // We silence the assert here because we might have a valid entity that is dead.
                        //DebugTools.Assert(mind.CurrentEntity == null, "a mind's current entity was deleted without updating the mind");
                        SpawnInLobbyWaitDb();
// ES END
                    }
                    else
                    {
                        if (_playerManager.SetAttachedEntity(session, mind.CurrentEntity))
                        {
                            PlayerJoinGame(session);
                        }
                        else
                        {
                            Log.Error(
                                $"Failed to attach player {session} with mind {ToPrettyString(mindId)} to its current entity {ToPrettyString(mind.CurrentEntity)}");
// ES START
                            SpawnInLobbyWaitDb();
// ES END
                        }
                    }

                    _adminLogger.Add(LogType.Connection, LogImpact.Low, $"User {args.Session:Player} attached to {(args.Session.AttachedEntity != null ? ToPrettyString(args.Session.AttachedEntity) : "nothing"):entity} connected to the game.");

                    break;
                }

                case SessionStatus.Disconnected:
                {
                    _chatManager.SendAdminAnnouncement(Loc.GetString("player-leave-message", ("name", args.Session.Name)));
                    if (mindId != null)
                    {
                        _pvsOverride.RemoveSessionOverride(mindId.Value, session);
                    }

                    _userDb.ClientDisconnected(session);

                    _adminLogger.Add(LogType.Connection, LogImpact.Low, $"User {args.Session:Player} attached to {(args.Session.AttachedEntity != null ? ToPrettyString(args.Session.AttachedEntity) : "nothing"):entity} disconnected from the game.");
                    break;
                }
            }
            //When the status of a player changes, update the server info text
            UpdateInfoText();

            async void SpawnWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                SpawnPlayer(session, EntityUid.Invalid);
            }

            async void SpawnObserverWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                JoinAsObserver(session);
            }
// ES START
            async void SpawnInLobbyWaitDb()
            {
                try
                {
                    await _userDb.WaitLoadComplete(session);
                }
                catch (OperationCanceledException)
                {
                    // Bail, user must've disconnected or something.
                    Log.Debug($"Database load cancelled while waiting to spawn {session}");
                    return;
                }

                if (LobbyEnabled)
                {
                    PlayerJoinLobby(session, attachCharacter: true);
                }
                else
                {
                    _stagehand.SpawnStagehand(session);
                }
            }
// ES END

            async void AddPlayerToDb(Guid id)
            {
                if (RoundId != 0 && _runLevel != GameRunLevel.PreRoundLobby)
                {
                    await _db.AddRoundPlayers(RoundId, id);
                }
            }
        }

        public HumanoidCharacterProfile GetPlayerProfile(ICommonSession p)
        {
            return (HumanoidCharacterProfile) _prefsManager.GetPreferences(p.UserId).SelectedCharacter;
        }

        public void PlayerJoinGame(ICommonSession session, bool silent = false)
        {
            if (!silent)
                _chatManager.DispatchServerMessage(session, Loc.GetString("game-ticker-player-join-game-message"));
// ES START
            _joinedPlayers.Add(session.UserId);
// ES SEND
            _playerGameStatuses[session.UserId] = PlayerGameStatus.JoinedGame;
            _db.AddRoundPlayers(RoundId, session.UserId);

            if (_adminManager.HasAdminFlag(session, AdminFlags.Admin))
            {
                if (_allPreviousGameRules.Count > 0)
                {
                    var rulesMessage = GetGameRulesListMessage(true);
                    _chatManager.SendAdminAnnouncementMessage(session, Loc.GetString("starting-rule-selected-preset", ("preset", rulesMessage)));
                }
            }

            RaiseNetworkEvent(new TickerJoinGameEvent(), session.Channel);
        }

// ES START
        public bool PlayerJoinLobby(ICommonSession session, bool attachCharacter = false)
        {
            if (!LobbyEnabled)
                return false;

            if (attachCharacter)
                AttachPlayerToLobbyCharacter(session);
// ES END

            _playerGameStatuses[session.UserId] = LobbyEnabled ? PlayerGameStatus.NotReadyToPlay : PlayerGameStatus.ReadyToPlay;
            _db.AddRoundPlayers(RoundId, session.UserId);

            var client = session.Channel;
            RaiseNetworkEvent(new TickerJoinLobbyEvent(), client);
            RaiseNetworkEvent(GetStatusMsg(session), client);
            RaiseNetworkEvent(GetInfoMsg(), client);
            RaiseLocalEvent(new PlayerJoinedLobbyEvent(session));
// ES START
            return true;
// ES END
        }

        private void ReqWindowAttentionAll()
        {
            RaiseNetworkEvent(new RequestWindowAttentionEvent());
        }
    }

    public sealed class PlayerJoinedLobbyEvent : EntityEventArgs
    {
        public readonly ICommonSession PlayerSession;

        public PlayerJoinedLobbyEvent(ICommonSession playerSession)
        {
            PlayerSession = playerSession;
        }
    }
}
