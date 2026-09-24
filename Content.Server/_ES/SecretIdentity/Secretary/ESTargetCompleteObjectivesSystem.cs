using System.Linq;
using Content.Server._ES.SecretIdentity.Secretary.Components;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind.Components;
using Content.Shared.Whitelist;
using Robust.Shared.Utility;

namespace Content.Server._ES.SecretIdentity.Secretary;

/// <summary>
/// This handles <see cref="ESTargetCompleteOwnedObjectiveComponent"/>
/// </summary>
public sealed partial class ESTargetCompleteObjectivesSystem : ESBaseTargetObjectiveSystem<ESTargetCompleteOwnedObjectiveComponent>
{
    [Dependency] private EntityWhitelistSystem _entityWhitelist = default!;

    public override Type[] TargetRelayComponents { get; } = [typeof(ESTargetCompleteOwnedObjectiveMarkerComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESObjectiveProgressChangedEvent>(OnObjectiveProgressChanged);
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

    protected override void OnTargetChanged(Entity<ESTargetCompleteOwnedObjectiveComponent> ent, ref ESObjectiveTargetChangedEvent args)
    {
        base.OnTargetChanged(ent, ref args);

        if (!args.NewTarget.HasValue || !MindSys.TryGetMind(args.NewTarget.Value, out var mind))
            return;

        foreach (var objective in GetTargetingObjectives(args.NewTarget.Value))
        {
            objective.Comp.TargetMind = mind;
            RefreshShouldAnnounceProgress(objective);
        }
    }

    private void OnTargetMindGotAdded(Entity<ESTargetCompleteOwnedObjectiveMarkerComponent> ent, ref MindAddedMessage args)
    {
        foreach (var objective in GetTargetingObjectives(ent))
        {
            objective.Comp.TargetMind = args.Mind;
            RefreshShouldAnnounceProgress(objective);
        }
    }

    private void OnObjectivesChanged(Entity<ESTargetCompleteOwnedObjectiveMarkerComponent> ent, ref ESObjectivesChangedEvent args)
    {
        foreach (var objective in GetTargetingObjectives(ent))
        {
            RefreshShouldAnnounceProgress(objective);
        }

        RefreshTargetingObjectives(ent);
    }

    private void OnValidateCandidates(Entity<ESTargetCompleteOwnedObjectiveComponent> ent, ref ESValidateObjectiveTargetCandidates args)
    {
        if (!MindSys.TryGetMind(args.Candidate, out var mind))
            return;

        if (!GetRelevantObjectives(ent, mind.Value).Any())
            args.Invalidate();
    }

    private void RefreshShouldAnnounceProgress(Entity<ESTargetCompleteOwnedObjectiveComponent> ent)
    {
        if (ent.Comp.TargetMind is not { } mind)
            return;

        var dontAnnounce = false;
        foreach (var objective in GetRelevantObjectives(ent, mind))
        {
            if (ObjectivesSys.ShouldAnnounceProgress(objective.AsNullable()))
                continue;
            dontAnnounce = true;
            break;
        }

        ObjectivesSys.SetShouldAnnounceProgress(ent.Owner, !dontAnnounce);
    }

    private IEnumerable<Entity<ESObjectiveComponent>> GetRelevantObjectives(
        Entity<ESTargetCompleteOwnedObjectiveComponent> ent,
        EntityUid mind)
    {
        foreach (var objective in ObjectivesSys.GetOwnedObjectives(mind))
        {
            if (_entityWhitelist.IsWhitelistPass(ent.Comp.ObjectiveBlacklist, objective))
                continue;

            yield return objective;
        }
    }

    protected override void GetObjectiveProgress(Entity<ESTargetCompleteOwnedObjectiveComponent> ent, ref ESGetObjectiveProgressEvent args)
    {
        if (ent.Comp.TargetMind is not { } mind)
        {
            args.Progress = ent.Comp.DefaultProgress;
            return;
        }

        var incomplete = GetRelevantObjectives(ent, mind)
            .Any(objective => !ObjectivesSys.IsCompleted(objective.AsNullable()));

        args.Progress = !incomplete ^ ent.Comp.Invert
            ? 1
            : 0;
    }
}
