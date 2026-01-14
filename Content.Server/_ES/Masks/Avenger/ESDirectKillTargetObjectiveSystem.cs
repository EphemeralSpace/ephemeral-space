using Content.Server._ES.Masks.Avenger.Components;
using Content.Server.KillTracking;
using Content.Shared._ES.Objectives.Target;

namespace Content.Server._ES.Masks.Avenger;

public sealed class ESDirectKillTargetObjectiveSystem : ESBaseTargetObjectiveSystem<ESDirectKillTargetObjectiveComponent>
{
    public override Type[] TargetRelayComponents { get; } = [typeof(ESDirectKillTargetObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESDirectKillTargetObjectiveMarkerComponent, KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(Entity<ESDirectKillTargetObjectiveMarkerComponent> ent, ref KillReportedEvent args)
    {
        if (args.Primary is not KillPlayerSource source ||
            !MindSys.TryGetMind(source.PlayerId, out var mind))
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESDirectKillTargetObjectiveComponent>(mind.Value.Owner))
        {
            if (TargetObjective.GetTargetOrNull(objective.Owner) != args.Entity)
                continue;

            ObjectivesSys.AdjustObjectiveCounter(objective.Owner);
        }
    }
}
