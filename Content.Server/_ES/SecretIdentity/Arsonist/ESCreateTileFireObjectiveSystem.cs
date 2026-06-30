using Content.Server._ES.SecretIdentity.Arsonist.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.Arsonist;

public sealed class ESCreateTileFireObjectiveSystem : ESBaseObjectiveSystem<ESCreateTileFireObjectiveComponent>
{
    public override Type[] RelayComponents { get; } = [typeof(ESTileFireRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESCreateTileFireObjectiveComponent, ESBodyCreatedTileFireEvent>(OnCreatedTileFire);
        SubscribeLocalEvent<ESCreateTileFireObjectiveComponent, ESBodyExtinguishedTileFireEvent>(OnExtinguishedTileFire);
    }

    private void OnCreatedTileFire(Entity<ESCreateTileFireObjectiveComponent> ent, ref ESBodyCreatedTileFireEvent args)
    {
        ObjectivesSys.AdjustObjectiveCounter(ent.Owner);
    }

    private void OnExtinguishedTileFire(Entity<ESCreateTileFireObjectiveComponent> ent, ref ESBodyExtinguishedTileFireEvent args)
    {
        ObjectivesSys.AdjustObjectiveCounter(ent.Owner, -1);
    }
}
