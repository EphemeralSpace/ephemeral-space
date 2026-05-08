using Content.Server.Administration;
using Content.Server.Chat.Managers;
using Content.Server.Doors.Systems;
using Content.Server.GameTicking;
using Content.Server.GameTicking.Events;
using Content.Server.Preferences.Managers;
using Content.Shared._ES.CCVar;
using Content.Shared._ES.Lobby;
using Content.Shared._ES.Lobby.Components;
using Content.Shared.Administration;
using Content.Shared.Alert;
using Content.Shared.Doors.Components;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.GameObjects;
using Robust.Server.Player;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Toolshed;
using Robust.Shared.Utility;

namespace Content.Server._ES.Lobby;

/// <summary>
/// handles serverside diegetic lobby stuff, notably readying on trigger and closing the lobby
/// </summary>
public sealed partial class ESDiegeticLobbySystem : ESSharedDiegeticLobbySystem
{
    [Dependency] private IConfigurationManager _cfg = default!;
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IServerPreferencesManager _preferences = default!;
    [Dependency] private IChatManager _chat = default!;
    [Dependency] private AlertsSystem _alerts = default!;
    [Dependency] private GameTicker _ticker = default!;
    [Dependency] private TransformSystem _xform = default!;
    [Dependency] private DoorSystem _door = default!;

    private static readonly ProtoId<AlertPrototype> NotReadiedAlert = "ESNotReadiedUp";

    private readonly Dictionary<ProtoId<JobPrototype>, int> _readiedJobCounts = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTheatergoerMarkerComponent, ComponentInit>(OnTheatergoerInit);
        SubscribeLocalEvent<RoundStartingEvent>(OnRoundStarting);
        SubscribeLocalEvent<ESLobbyWorldCreatedEvent>(OnLobbyWorldCreated);
        // buckling (to observe) is handled on the client
        // opens the observe window, which just calls the observe command if u click yes
        // and then the actual behavior is just in that command.

        // unbuckling is handled here though. see the shared version (and handler below)

        _player.PlayerStatusChanged += (_, args) =>
        {
            if (args.NewStatus is SessionStatus.Disconnected or SessionStatus.Zombie or SessionStatus.Connecting)
                return;

            var ev = new ESUpdatePlayerReadiedJobCounts(_readiedJobCounts);
            RaiseNetworkEvent(ev, args.Session);
        };
        _preferences.ESOnAfterCharacterUpdated += RefreshReadiedJobCounts;

        _cfg.OnValueChanged(ESCVars.LobbyClosed, UpdateLobbyClosedStatus);
    }

    private void OnLobbyWorldCreated(ref ESLobbyWorldCreatedEvent ev)
    {
        // if lobby should be closed when we create it,
        // immediately make sure the doors are closed
        // if it should be disabled, we dont need to do anything special
        if (_cfg.GetCVar(ESCVars.LobbyClosed))
            UpdateLobbyClosedStatus(true);
    }

    private void UpdateLobbyClosedStatus(bool closing)
    {
        _ticker.PauseStart(closing);

        // Close/open doors
        var barrierSpawnQuery = EntityQueryEnumerator<ESLobbyClosingDoorComponent, DoorComponent>();
        while (barrierSpawnQuery.MoveNext(out var uid, out _, out var door))
        {
            _ = closing ? _door.TryClose(uid, door) : _door.TryOpen(uid, door);
        }

        if (closing && _ticker.RunLevel == GameRunLevel.PreRoundLobby)
        {
            // If there are any theatergoers, move them back to the lobby spawn point
            var theatergoerQuery = EntityQueryEnumerator<ESTheatergoerMarkerComponent, TransformComponent, MetaDataComponent>();
            while (theatergoerQuery.MoveNext(out var uid, out _, out var xform, out var metadata))
            {
                if (_ticker.GetTheatergoerSpawnPoint() is not { EntityId.Id: > 0 } coords)
                    continue;

                _xform.SetCoordinates((uid, xform, metadata), coords);
            }
        }

        _chat.DispatchServerAnnouncement(closing
            ? Loc.GetString("es-lobby-closed-announcement")
            : Loc.GetString("es-lobby-open-announcement"));
    }

    protected override void OnPlayerReadyToggled(ref ESOnPlayerReadyToggled ev)
    {
        base.OnPlayerReadyToggled(ref ev);
        RefreshReadiedJobCounts();
    }

    private void RefreshReadiedJobCounts()
    {
        _readiedJobCounts.Clear();

        foreach (var session in _player.Sessions)
        {
            if (session.Status is SessionStatus.Disconnected or SessionStatus.Zombie)
                continue;
            if (!_ticker.PlayerGameStatuses.TryGetValue(session.UserId, out var status) ||
                status != PlayerGameStatus.ReadyToPlay)
                continue;
            if (!_preferences.TryGetCachedPreferences(session.UserId, out var preferences))
                continue;

            var profile = (HumanoidCharacterProfile)preferences.SelectedCharacter;

            foreach (var (job, priority) in profile.JobPriorities)
            {
                if (priority == JobPriority.Never)
                    continue;

                var existing = _readiedJobCounts.GetOrNew(job);
                _readiedJobCounts[job] = existing + 1; // add one
            }
        }

        var ev = new ESUpdatePlayerReadiedJobCounts(_readiedJobCounts);
        RaiseNetworkEvent(ev);
    }

    protected override void OnTriggerCollided(Entity<ESReadyTriggerMarkerComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<ESTheatergoerMarkerComponent>(args.OtherEntity)
            || !TryComp<ActorComponent>(args.OtherEntity, out var actor)
            // idk why someone would do this but like .
            || ent.Comp.Behavior is not (PlayerGameStatus.NotReadyToPlay or PlayerGameStatus.ReadyToPlay))
            return;

        switch (_ticker.RunLevel)
        {
            case GameRunLevel.PreRoundLobby:
                _ticker.ToggleReady(actor.PlayerSession, ent.Comp.Behavior);
                break;
            case GameRunLevel.InRound:
                // handled on the client
                // (opens the spawning menu)
            case GameRunLevel.PostRound:
                break;
        }
    }

    // add unreadied alert by default
    private void OnTheatergoerInit(Entity<ESTheatergoerMarkerComponent> ent, ref ComponentInit args)
    {
        if (_ticker.RunLevel is GameRunLevel.PreRoundLobby)
            _alerts.ShowAlert(ent.Owner, NotReadiedAlert);
    }

    private void OnRoundStarting(RoundStartingEvent ev)
    {
        var query = EntityQueryEnumerator<ESTheatergoerMarkerComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            Actions.RemoveAction(uid, comp.ConfigurePrefsActionEntity);
            comp.ConfigurePrefsActionEntity = null;
        }
    }
}

[ToolshedCommand(Name = "lobby"), AdminCommand(AdminFlags.Server)]
public sealed partial class LobbyCommands : ToolshedCommand
{
    [Dependency] private IConfigurationManager _cfg = default!;

    [CommandImplementation("toggleclosed")]
    public void ToggleClosed()
    {
        var closed = _cfg.GetCVar(ESCVars.LobbyClosed);
        _cfg.SetCVar(ESCVars.LobbyClosed, !closed);
    }
}
