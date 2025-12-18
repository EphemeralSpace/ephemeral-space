using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared.Mobs;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESSurviveObjectiveSystem : ESBaseObjectiveSystem<ESSurviveObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESMobStateRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSurviveObjectiveComponent, ESMobStateChanged>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESSurviveObjectiveComponent> ent, ref ESMobStateChanged args)
    {
        ObjectivesSys.SetObjectiveCounter(ent.Owner,
            args.NewMobState switch
            {
                MobState.Dead => 0f,
                _ => 1f,
            });
    }
}
