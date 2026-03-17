using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._ES.Objectives.Target;

public sealed class ESShareTargetObjectiveSystem : ESBaseObjectiveSystem<ESShareTargetObjectiveComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly ESTargetObjectiveSystem _targetObjective = default!;

    protected override void InitializeObjective(Entity<ESShareTargetObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        foreach (var objective in ObjectivesSys.GetObjectives<ESTargetObjectiveComponent>(args.Holder.AsNullable()))
        {
            if (_entityWhitelist.IsWhitelistFail(ent.Comp.ObjectiveWhitelist, objective))
                continue;

            _targetObjective.SetTarget(ent.Owner, _targetObjective.GetTargetOrNull(objective.AsNullable()));
            break;
        }
    }
}
