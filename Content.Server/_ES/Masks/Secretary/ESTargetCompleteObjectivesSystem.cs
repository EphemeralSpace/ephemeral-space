using Content.Server._ES.Masks.Secretary.Components;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

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
        if (HasComp<ESTargetCompleteOwnedObjectiveComponent>(ev.Objective))
            return;

        // This really shouldn't be necessary but I don't want
        // to accidentally create an infinite loop here if something's messed up.
        if (_loop)
        {
            DebugTools.Assert($"Infinite loop detected in {nameof(ESTargetCompleteObjectivesSystem)}!");
            return;
        }

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

        var incomplete = false;
        foreach (var objective in ObjectivesSys.GetOwnedObjectives(mind))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            incomplete |= !ObjectivesSys.IsCompleted(objective.AsNullable());
        }

        args.Progress = !incomplete ^ ent.Comp.Invert
            ? 1
            : 0;
    }
}
