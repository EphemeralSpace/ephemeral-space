using Content.Shared._ES.Lobby.Components;
using Content.Shared.Actions;
using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking;
using Content.Shared.Roles;
using Robust.Shared.Physics.Events;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Lobby;

// see client/server
public abstract class ESSharedDiegeticLobbySystem : EntitySystem
{
    [Dependency] private readonly SharedActionsSystem _actions = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESReadyTriggerMarkerComponent, StartCollideEvent>(OnTriggerCollided);
        SubscribeLocalEvent<ESTheatergoerMarkerComponent, UnbuckledEvent>(OnTheatergoerUnbuckled);
        SubscribeLocalEvent<ESOnPlayerReadyToggled>(OnPlayerReadyToggled);
    }

    protected virtual void OnPlayerReadyToggled(ref ESOnPlayerReadyToggled ev)
    {
        if (ev.Player.AttachedEntity is not { } entity)
            return;
        if (!TryComp<ESTheatergoerMarkerComponent>(entity, out var theaterGoer))
            return;

        var ready = ev.GameStatus == PlayerGameStatus.ReadyToPlay;
        if (ready)
        {
            _actions.AddAction(entity, ref theaterGoer.ConfigurePrefsActionEntity, theaterGoer.ConfigurePrefsAction);
        }
        else
        {
            _actions.RemoveAction(entity, theaterGoer.ConfigurePrefsActionEntity);
            theaterGoer.ConfigurePrefsActionEntity = null;
        }
        Dirty(entity, theaterGoer);
    }

    protected abstract void OnTheatergoerUnbuckled(Entity<ESTheatergoerMarkerComponent> ent, ref UnbuckledEvent args);

    protected abstract void OnTriggerCollided(Entity<ESReadyTriggerMarkerComponent> ent, ref StartCollideEvent args);
}

[Serializable, NetSerializable]
public sealed class ESUpdatePlayerReadiedJobCounts(Dictionary<ProtoId<JobPrototype>, int> jobs) : EntityEventArgs
{
    public Dictionary<ProtoId<JobPrototype>, int> ReadiedJobCounts = jobs;
}
