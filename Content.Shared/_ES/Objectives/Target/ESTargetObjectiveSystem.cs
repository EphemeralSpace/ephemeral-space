using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Auditions.Components;
using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Mind;
using Content.Shared.Roles.Jobs;
using JetBrains.Annotations;
using Robust.Shared.Random;

namespace Content.Shared._ES.Objectives.Target;

public sealed class ESTargetObjectiveSystem : EntitySystem
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly SharedJobSystem _job = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedMindSystem _mind = default!;
    [Dependency] private readonly ESSharedObjectiveSystem _objective = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetObjectiveComponent, ESInitializeObjectiveEvent>(OnInitializeObjective);
        SubscribeLocalEvent<ESTargetObjectiveMarkerComponent, ComponentShutdown>(OnTargetShutdown);
    }

    private void OnInitializeObjective(Entity<ESTargetObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        if (!TryGetCandidate(args.Holder, ent, out var candidate))
            return;

        SetTarget(ent.AsNullable(), candidate);
    }

    private void OnTargetShutdown(Entity<ESTargetObjectiveMarkerComponent> ent, ref ComponentShutdown args)
    {
        foreach (var objective in ent.Comp.Objectives)
        {
            if (TryComp<ESTargetObjectiveComponent>(objective, out var comp))
                SetTarget((objective, comp), null);
        }
    }

    public bool TryGetCandidate(
        Entity<ESObjectiveHolderComponent> holder,
        Entity<ESTargetObjectiveComponent> ent,
        [NotNullWhen(true)] out EntityUid? candidate)
    {
        candidate = null;
        var candidates = GetTargetCandidates(holder, ent).ToList();
        if (candidates.Count == 0)
            return false;

        candidate = _random.Pick(candidates);
        return true;
    }

    public IEnumerable<EntityUid> GetTargetCandidates(Entity<ESObjectiveHolderComponent> holder, Entity<ESTargetObjectiveComponent> ent)
    {
        var otherTargets = new HashSet<EntityUid>();
        foreach (var objective in _objective.GetObjectives<ESTargetObjectiveComponent>(holder.AsNullable()))
        {
            if (objective.Comp.Target is { } target)
                otherTargets.Add(target);
        }

        var ev = new ESGetObjectiveTargetCandidates(holder, []);
        RaiseLocalEvent(ent, ref ev);

        foreach (var candidate in ev.Candidates)
        {
            // Don't share targets between multiple objectives
            // This technically isn't necessary for ALL targeted objectives,
            // but i think for gameplay purposes there really isnt a reason to allow it.
            if (otherTargets.Contains(candidate))
                continue;

            var checkEv = new ESValidateObjectiveTargetCandidates(holder, candidate);
            RaiseLocalEvent(ent, ref checkEv);

            if (checkEv.Valid)
                yield return candidate;
        }
    }

    public bool TryGetTarget(Entity<ESTargetObjectiveComponent?> ent, [NotNullWhen(true)] out EntityUid? target)
    {
        target = null;
        if (!Resolve(ent, ref ent.Comp))
            return false;

        target = ent.Comp.Target;
        return target != null;
    }

    public EntityUid? GetTargetOrNull(Entity<ESTargetObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return null;

        return ent.Comp.Target;
    }

    /// <summary>
    /// Sets the target for a given <see cref="ESTargetObjectiveComponent"/>
    /// </summary>
    public void SetTarget(Entity<ESTargetObjectiveComponent?> ent, EntityUid? target)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var oldTarget = ent.Comp.Target;
        if (oldTarget.HasValue)
        {
            if (TryComp<ESTargetObjectiveMarkerComponent>(oldTarget, out var targetComp))
                targetComp.Objectives.Remove(ent);
        }

        ent.Comp.Target = target;

        if (target != null)
        {
            if (ent.Comp.Title != null)
            {
                var name = Name(target.Value);
                var job = string.Empty;
                if (_mind.TryGetMind(target.Value, out var mind, out _))
                {
                    if (TryComp<ESCharacterComponent>(mind, out var characterComponent))
                        name = characterComponent.Name;
                    _job.MindTryGetJobName(mind, out job);
                }

                var title = Loc.GetString(ent.Comp.Title, ("targetName", name), ("job", job));
                _metaData.SetEntityName(ent, title);
            }

            var comp = EnsureComp<ESTargetObjectiveMarkerComponent>(target.Value);
            comp.Objectives.Add(ent);
        }

        var ev = new ESObjectiveTargetChangedEvent(oldTarget, target);
        RaiseLocalEvent(ent, ref ev);

        _objective.RefreshObjectiveProgress(ent.Owner);
    }

    /// <summary>
    /// Returns the objectives that are targeting a given entity.
    /// </summary>
    [PublicAPI]
    public IEnumerable<EntityUid> GetTargetingObjectives(Entity<ESTargetObjectiveMarkerComponent?> ent)
    {
        return GetTargetingObjectives<ESObjectiveComponent>(ent).Select(e => e.Owner);
    }

    /// <summary>
    /// Returns the objectives that are targeting a given entity, filtered by a particular component
    /// </summary>
    public IEnumerable<Entity<TComponent>> GetTargetingObjectives<TComponent>(Entity<ESTargetObjectiveMarkerComponent?> ent)
        where TComponent : Component
    {
        if (!Resolve(ent, ref ent.Comp, false))
            yield break;

        foreach (var objective in ent.Comp.Objectives)
        {
            if (TryComp<TComponent>(objective, out var comp))
                yield return (objective, comp);
        }
    }
}
