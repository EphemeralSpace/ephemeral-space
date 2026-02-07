using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Kill.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared._ES.Objectives.Kill;

public sealed class ESKillTargetObjectiveSystem : ESBaseTargetObjectiveSystem<ESKillTargetObjectiveComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(ESKillTargetObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESKillTargetObjectiveMarkerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESKillTargetObjectiveMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }

    protected override void GetObjectiveProgress(Entity<ESKillTargetObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        if (!TargetObjective.TryGetTarget(ent.Owner, out var target))
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        if (!TryComp<MobStateComponent>(target.Value, out var mobState))
            return;

        args.Progress = mobState.CurrentState switch
        {
            MobState.Alive => 0f,
            MobState.Critical => 0.5f,
            MobState.Dead => 1,
            _ => 1,
        };
    }
}
