using Content.Server._ES.SecretIdentity.Objectives.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared.Mobs;

namespace Content.Server._ES.SecretIdentity.Objectives;

public sealed class ESMobStateObjectiveSystem : ESBaseObjectiveSystem<ESMobStateObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESMobStateRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMobStateObjectiveComponent, ESMobStateChanged>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESMobStateObjectiveComponent> ent, ref ESMobStateChanged args)
    {
        var success = ent.Comp.DesiredStates.Contains(args.NewMobState);
        ObjectivesSys.SetObjectiveCounter(ent.Owner, success ? 1f : 0f);
    }
}
