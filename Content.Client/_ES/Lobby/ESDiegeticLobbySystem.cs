using Content.Client._ES.Station.Ui;
using Content.Client.GameTicking.Managers;
using Content.Shared._ES.Lobby;
using Content.Shared._ES.Lobby.Components;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Shared.Physics.Events;
using Robust.Shared.Timing;

namespace Content.Client._ES.Lobby;

/// <inheritdoc/>
public sealed partial class ESDiegeticLobbySystem : ESSharedDiegeticLobbySystem
{
    [Dependency] private IPlayerManager _player = default!;
    [Dependency] private IGameTiming _timing = default!;
    [Dependency] private ClientGameTicker _ticker = default!;

    private ESJobPrefsWindow? _jobPrefsWindow;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TickerJoinGameEvent>(OnTickerJoinGame);
        SubscribeLocalEvent<ESTheatergoerMarkerComponent, ESConfigurePrefsToggleActionEvent>(OnConfigurePrefsToggleAction);
    }

    private void OnTickerJoinGame(TickerJoinGameEvent ev)
    {
        _jobPrefsWindow?.Close();
    }

    protected override void OnTriggerCollided(Entity<ESReadyTriggerMarkerComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<ESTheatergoerMarkerComponent>(args.OtherEntity)
            || args.OtherEntity != _player.LocalEntity
            || !_timing.IsFirstTimePredicted
            || _ticker.IsGameStarted)
            return;

        // open prefs window on the first time we enter stage after connecting
        if (ent.Comp.Behavior is PlayerGameStatus.ReadyToPlay)
        {
            if (_jobPrefsWindow != null)
                return;

            _jobPrefsWindow = new ESJobPrefsWindow();
            _jobPrefsWindow.OpenCentered();
        }
        else
        {
            _jobPrefsWindow?.Close();
        }
    }

    private void OnConfigurePrefsToggleAction(Entity<ESTheatergoerMarkerComponent> ent, ref ESConfigurePrefsToggleActionEvent args)
    {
        _jobPrefsWindow ??= new ESJobPrefsWindow();
        if (!_jobPrefsWindow.IsOpen)
            _jobPrefsWindow.OpenCentered();
        else
            _jobPrefsWindow.Close();

        args.Handled = true;
    }
}
