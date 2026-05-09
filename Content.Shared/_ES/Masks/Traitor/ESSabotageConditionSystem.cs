using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.Objectives;
using Content.Shared.Mind;
using Content.Shared.Whitelist;

namespace Content.Shared._ES.Masks.Traitor;

/// <summary>
/// This handles <see cref="ESSabotageConditionComponent"/>
/// </summary>
public sealed partial class ESSabotageConditionSystem : ESBaseObjectiveSystem<ESSabotageConditionComponent>
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;
    [Dependency] private SharedMindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSabotageCompletedEvent>(OnSabotageCompleted);
    }

    private void OnSabotageCompleted(ref ESSabotageCompletedEvent args)
    {
        if (!_mind.TryGetMind(args.User, out var mindUid, out _))
            return;
        foreach (var objective in ObjectivesSys.GetObjectives<ESSabotageConditionComponent>(mindUid))
        {
            if (_entityWhitelist.IsWhitelistFail(objective.Comp.Whitelist, args.Target))
                continue;

            ObjectivesSys.AdjustObjectiveCounter(objective.Owner);
        }
    }
}
