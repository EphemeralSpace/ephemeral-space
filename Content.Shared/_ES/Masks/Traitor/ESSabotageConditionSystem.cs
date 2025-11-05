using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared.Mind;
using Content.Shared.Objectives.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._ES.Masks.Traitor;

/// <summary>
/// This handles <see cref="ESSabotageConditionComponent"/>
/// </summary>
public sealed class ESSabotageConditionSystem : EntitySystem
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESSabotageConditionComponent, ObjectiveGetProgressEvent>(OnGetProgress);
        SubscribeLocalEvent<ESSabotageCompletedEvent>(OnSabotageCompleted);
    }

    private void OnGetProgress(Entity<ESSabotageConditionComponent> ent, ref ObjectiveGetProgressEvent args)
    {
        args.Progress = ent.Comp.Completed ? 1f : 0f;
    }

    private void OnSabotageCompleted(ref ESSabotageCompletedEvent args)
    {
        if (!_mind.TryGetMind(args.User, out var mindUid, out var mindComp))
            return;
        foreach (var objective in _mind.ESGetObjectivesComp<ESSabotageConditionComponent>((mindUid, mindComp)))
        {
            if (objective.Comp.Completed)
                continue;

            if (_entityWhitelist.IsWhitelistFail(objective.Comp.Whitelist, args.Target))
                continue;

            objective.Comp.Completed = true;
        }
    }
}
