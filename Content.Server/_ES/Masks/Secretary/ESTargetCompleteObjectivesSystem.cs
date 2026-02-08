using Content.Server._ES.Masks.Secretary.Components;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;

namespace Content.Server._ES.Masks.Secretary;

/// <summary>
/// This handles <see cref="ESTargetCompleteOwnedObjectiveComponent"/>
/// </summary>
public sealed class ESTargetCompleteObjectivesSystem : ESBaseTargetObjectiveSystem<ESTargetCompleteOwnedObjectiveComponent>
{
    [Dependency] private readonly EntityWhitelistSystem _entityWhitelist = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(ESTargetCompleteOwnedObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnObjectiveProgressChanged);
        SubscribeLocalEvent<ESTargetCompleteOwnedObjectiveMarkerComponent, MapInitEvent>(OnMapInit);
        SubscribeLocalEvent<ESTargetCompleteOwnedObjectiveMarkerComponent, MindAddedMessage>(OnTargetMindGotAdded);
        SubscribeLocalEvent<ESTargetCompleteOwnedObjectiveMarkerComponent, ESObjectivesChangedEvent>(OnObjectivesChanged);

        SubscribeLocalEvent<ESTargetCompleteOwnedObjectiveComponent, ESValidateObjectiveTargetCandidates>(OnValidateCandidates);
    }

    private bool _loop;

    private void OnObjectiveProgressChanged(ref ESObjectiveProgressChangedEvent ev)
    {
        // This really shouldn't be necessary but I don't want
        // to accidentally create an infinite loop here if something's messed up.
        if (_loop)
            return;

        _loop = true;
        // I would prefer to not have a global sub here but it's pretty much impossible to do otherwise
        ObjectivesSys.RefreshObjectiveProgress<ESTargetCompleteOwnedObjectiveComponent>();
        _loop = false;
    }

    private void OnMapInit(Entity<ESTargetCompleteOwnedObjectiveMarkerComponent> ent, ref MapInitEvent args)
    {
        if (!MindSys.TryGetMind(ent, out var mind, out _))
            return;

        foreach (var objective in GetTargetingObjectives(ent))
        {
            objective.Comp.TargetMind = mind;
        }
    }

    private void OnTargetMindGotAdded(Entity<ESTargetCompleteOwnedObjectiveMarkerComponent> ent, ref MindAddedMessage args)
    {
        foreach (var objective in GetTargetingObjectives(ent))
        {
            objective.Comp.TargetMind = args.Mind;
        }
    }

    private void OnObjectivesChanged(Entity<ESTargetCompleteOwnedObjectiveMarkerComponent> ent, ref ESObjectivesChangedEvent args)
    {
        RefreshTargetingObjectives(ent);
    }

    private void OnValidateCandidates(Entity<ESTargetCompleteOwnedObjectiveComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        if (!MindSys.TryGetMind(args.Candidate, out var mindId, out _))
            return;

        var objectiveCount = 0;
        foreach (var objective in ObjectivesSys.GetOwnedObjectives(mindId))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            ++objectiveCount;
        }

        if (objectiveCount <= 0)
            args.Invalidate();
    }

    protected override void GetObjectiveProgress(Entity<ESTargetCompleteOwnedObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        if (ent.Comp.TargetMind is not { } mind)
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        var objectiveSum = 0f;
        var progressSum = 0f;

        foreach (var objective in ObjectivesSys.GetOwnedObjectives(mind))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            objectiveSum += 1;
            progressSum += ObjectivesSys.GetProgress(objective.AsNullable());
        }

        if (objectiveSum == 0)
        {
            // you win? ig.
            args.Progress = 1f;
            return;
        }

        args.Progress = progressSum / objectiveSum;

        if (ent.Comp.Invert)
            args.Progress = 1 - args.Progress;
    }
}
