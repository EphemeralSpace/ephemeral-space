using Content.Server._ES.Masks.Objectives.Components;
using Content.Server.KillTracking;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Masks.Objectives;

/// <summary>
///     This handles the kill troupe objective.
/// </summary>
/// <seealso cref="ESKillTroupeObjectiveComponent"/>
public sealed class ESKillTroupeObjectiveSystem : ESBaseObjectiveSystem<ESKillTroupeObjectiveComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<KillReportedEvent>(OnKillReported);
    }

    private void OnKillReported(ref KillReportedEvent args)
    {
        if (args.Primary is not KillPlayerSource source ||
            !MindSys.TryGetMind(source.PlayerId, out var mind))
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESKillTroupeObjectiveComponent>(mind.Value.Owner))
        {
            if (!MaskSys.TryGetTroupe(args.Entity, out var troupe))
                return;

            if ((troupe == objective.Comp.Troupe) ^ objective.Comp.Invert)
                ObjectivesSys.AdjustObjectiveCounter(objective.Owner);
        }
    }
}
