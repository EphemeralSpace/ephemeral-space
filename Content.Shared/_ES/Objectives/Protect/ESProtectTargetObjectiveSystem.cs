using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Protect.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared.Mobs;
using Content.Shared.Mobs.Components;

namespace Content.Shared._ES.Objectives.Protect;

public sealed class ESProtectTargetObjectiveSystem : ESBaseTargetObjectiveSystem<ESProtectTargetObjectiveComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(ESProtectTargetObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESProtectTargetObjectiveMarkerComponent, MobStateChangedEvent>(OnMobStateChanged);
    }

    private void OnMobStateChanged(Entity<ESProtectTargetObjectiveMarkerComponent> ent, ref MobStateChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }

    protected override void GetObjectiveProgress(Entity<ESProtectTargetObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
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
            MobState.Alive => 1f,
            MobState.Critical => 0.5f,
            MobState.Dead => 0,
            _ => 1,
        };
    }
}
