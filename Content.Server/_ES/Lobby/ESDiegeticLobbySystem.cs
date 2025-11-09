using Content.Server.GameTicking;
using Content.Server.Preferences.Managers;
using Content.Shared._ES.Lobby;
using Content.Shared._ES.Lobby.Components;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking;
using Content.Shared.Preferences;
using Content.Shared.Roles;
using Robust.Server.Player;
using Robust.Shared.Enums;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Utility;

namespace Content.Server._ES.Lobby;

/// <summary>
/// handles serverside diegetic lobby stuff, notably readying on trigger
/// </summary>
public sealed class ESDiegeticLobbySystem : ESSharedDiegeticLobbySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IServerPreferencesManager _preferences = default!;
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    private static readonly ProtoId<AlertPrototype> NotReadiedAlert = "ESNotReadiedUp";

    private readonly Dictionary<ProtoId<JobPrototype>, int> _readiedJobCounts = new();

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTheatergoerMarkerComponent, ComponentInit>(OnTheatergoerInit);
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

    protected override void OnTheatergoerUnbuckled(Entity<ESTheatergoerMarkerComponent> ent, ref UnbuckledEvent args)
    {
        if (!HasComp<ESObserverChairComponent>(args.Strap.Owner)
            || !TryComp<ActorComponent>(ent.Owner, out var actor))
            return;

        _ticker.ToggleReady(actor.PlayerSession, PlayerGameStatus.NotReadyToPlay);
    }

    // add unreadied alert by default
    private void OnTheatergoerInit(Entity<ESTheatergoerMarkerComponent> ent, ref ComponentInit args)
    {
        if (_ticker.RunLevel is GameRunLevel.PreRoundLobby)
            _alerts.ShowAlert(ent.Owner, NotReadiedAlert);
    }
}
