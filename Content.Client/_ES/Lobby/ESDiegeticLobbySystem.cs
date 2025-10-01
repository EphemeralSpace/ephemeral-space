using Content.Client._ES.Spawning.Ui;
using Content.Client._ES.Station.Ui;
using Content.Client.GameTicking.Managers;
using Content.Client.Lobby.UI;
using Content.Shared._ES.Lobby;
using Content.Shared._ES.Lobby.Components;
using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking;
using Robust.Client.Player;
using Robust.Shared.Physics.Events;

namespace Content.Client._ES.Lobby;

/// <inheritdoc/>
public sealed class ESDiegeticLobbySystem : ESSharedDiegeticLobbySystem
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IDynamicTypeFactory _type = default!;
    [Dependency] private readonly ClientGameTicker _ticker = default!;

    private ObserveWarningWindow? _observeWindow;
    private ESJobPrefsWindow? _jobPrefsWindow;
    private ESSpawningWindow? _spawningWindow;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<TickerJoinGameEvent>(OnTickerJoinGame);
        SubscribeLocalEvent<ESTheatergoerMarkerComponent, BuckledEvent>(OnTheatergoerBuckled);
    }

    private void OnTickerJoinGame(TickerJoinGameEvent ev)
    {
        _spawningWindow?.Close();
        _jobPrefsWindow?.Close();
    }

    protected override void OnTriggerCollided(Entity<ESReadyTriggerMarkerComponent> ent, ref StartCollideEvent args)
    {
        if (!HasComp<ESTheatergoerMarkerComponent>(args.OtherEntity)
            || args.OtherEntity != _player.LocalEntity)
            return;

        if (ent.Comp.Behavior is not PlayerGameStatus.ReadyToPlay)
        {
            _spawningWindow?.Close();
            _jobPrefsWindow?.Close();
            return;
        }

        if (!_ticker.IsGameStarted)
        {
            if (_jobPrefsWindow?.IsOpen != true)
            {
                _jobPrefsWindow ??= new ESJobPrefsWindow();
                _jobPrefsWindow.OpenCentered();
            }
            return;
        }

        if (_spawningWindow?.IsOpen == true)
            return;
        _spawningWindow ??= new ESSpawningWindow();
        _spawningWindow.OpenCentered();
    }

    private void OnTheatergoerBuckled(Entity<ESTheatergoerMarkerComponent> ent, ref BuckledEvent args)
    {
        if (!HasComp<ESObserverChairComponent>(args.Strap.Owner)
            || ent.Owner != _player.LocalEntity)
            return;

        // different window closing behavior than above
        // because its fine to just reclose/open these but if they walk around into the ready
        // trigger multiple times we probably just want to keep the window open until they close it again
        _observeWindow?.Close();
        _observeWindow = _type.CreateInstance<ObserveWarningWindow>();
        _observeWindow?.OpenCentered();
    }

    protected override void OnTheatergoerUnbuckled(Entity<ESTheatergoerMarkerComponent> ent, ref UnbuckledEvent args)
    {
        if (ent.Owner != _player.LocalEntity)
            return;

        _observeWindow?.Close();
        _observeWindow = null;
    }
}
