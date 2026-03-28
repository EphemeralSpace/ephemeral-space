using Content.Server._ES.Masks;
using Content.Server._ES.Troupes.Parasite.Components;
using Content.Server.Mind;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Mind;

namespace Content.Server._ES.Troupes.Parasite;

public sealed class ESTroupeOutnumberObjectiveSystem : ESBaseObjectiveSystem<ESTroupeOutnumberObjectiveComponent>
{
    [Dependency] private readonly ESMaskSystem _mask = default!;
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESMaskChangedEvent>(OnMaskChanged);
        SubscribeLocalEvent<ESPlayerKilledEvent>(OnPlayerKilled);
    }

    private void OnMaskChanged(ref ESMaskChangedEvent ev)
    {
        ObjectivesSys.RefreshObjectiveProgress<ESTroupeOutnumberObjectiveComponent>();
    }

    private void OnPlayerKilled(ref ESPlayerKilledEvent ev)
    {
        ObjectivesSys.RefreshObjectiveProgress<ESTroupeOutnumberObjectiveComponent>();
    }

    protected override void GetObjectiveProgress(Entity<ESTroupeOutnumberObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        base.GetObjectiveProgress(ent, ref args);

        var troupeCount = 0;
        foreach (var mind in _mask.GetTroupeMembers(ent.Comp.Troupe))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++troupeCount;
        }

        var nonTroupeCount = 0;
        foreach (var mind in _mask.GetNotTroupeMembers(ent.Comp.Troupe))
        {
            if (!TryComp<MindComponent>(mind, out var mindComp))
                continue;

            if (!_mind.IsCharacterDeadIc(mindComp))
                ++nonTroupeCount;
        }

        if (troupeCount == 0)
            return; // default progress = 0

        var percentage = nonTroupeCount == 0 ? 1f : (float) troupeCount / (troupeCount + nonTroupeCount);
        args.Progress = percentage == 0 ? 0 : percentage / ent.Comp.TargetPercentage;
    }
}
