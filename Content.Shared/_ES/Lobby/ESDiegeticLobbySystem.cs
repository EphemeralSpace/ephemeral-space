using Content.Shared.Buckle.Components;
using Content.Shared.GameTicking;
using Robust.Shared.Physics.Events;
using Robust.Shared.Player;

namespace Content.Shared._ES.Lobby;

// see client/server
public abstract class ESSharedDiegeticLobbySystem : EntitySystem
{
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESReadyTriggerMarkerComponent, StartCollideEvent>(OnTriggerCollided);
        SubscribeLocalEvent<ESTheatergoerMarkerComponent, UnbuckledEvent>(OnTheatergoerUnbuckled);
    }

    protected abstract void OnTheatergoerUnbuckled(Entity<ESTheatergoerMarkerComponent> ent, ref UnbuckledEvent args);

    protected abstract void OnTriggerCollided(Entity<ESReadyTriggerMarkerComponent> ent, ref StartCollideEvent args);
}
