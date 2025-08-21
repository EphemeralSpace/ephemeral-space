using Content.Server.GameTicking;
using Content.Shared._ES.Lobby;
using Content.Shared.Alert;
using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Timing;

namespace Content.Server._ES.Lobby;

/// <summary>
/// handles serverside diegetic lobby stuff, notably readying on trigger
/// </summary>
public sealed class ESDiegeticLobbySystem : ESSharedDiegeticLobbySystem
{
    [Dependency] private readonly AlertsSystem _alerts = default!;
    [Dependency] private readonly GameTicker _ticker = default!;

    private static readonly ProtoId<AlertPrototype> NotReadiedAlert = "ESNotReadiedUp";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESTheatergoerMarkerComponent, ComponentInit>(OnTheatergoerInit);
        // buckling (to observe) is handled on the client
        // opens the observe window, which just calls the observe command if u click yes
        // and then the actual behavior is just in that command.

        // unbuckling is handled here though. see the shared version (and handler below)
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
