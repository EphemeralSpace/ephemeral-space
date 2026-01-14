using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared._ES.Mind;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Mind;
using Content.Shared.Mind.Components;
using JetBrains.Annotations;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared._ES.Objectives;

/// <summary>
/// Handles assignment and core logic of objectives for ES.
/// </summary>
public abstract partial class ESSharedObjectiveSystem : EntitySystem
{
    [Dependency] private readonly ISharedPlayerManager _player = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly EntityTableSystem _entityTable = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;
    [Dependency] private readonly SharedPvsOverrideSystem _pvsOverride = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        InitializeCounter();

        SubscribeLocalEvent<ESObjectiveHolderComponent, MindGotAddedEvent>(OnMindGotAdded);
        SubscribeLocalEvent<ESObjectiveHolderComponent, MindGotRemovedEvent>(OnMindGotRemoved);

        SubscribeLocalEvent<ESObjectiveHolderComponent, ESMindPlayerAttachedEvent>(OnPlayerAttached);
        SubscribeLocalEvent<ESObjectiveHolderComponent, ESMindPlayerDetachedEvent>(OnPlayerDetached);

        SubscribeLocalEvent<ESObjectiveComponent, EntityRenamedEvent>(OnObjectiveRenamed);
    }

    private void OnMindGotAdded(Entity<ESObjectiveHolderComponent> ent, ref MindGotAddedEvent args)
    {
        foreach (var objective in GetObjectives(ent.AsNullable()))
        {
            RaiseLocalEvent(objective, args);
        }
    }

    private void OnMindGotRemoved(Entity<ESObjectiveHolderComponent> ent, ref MindGotRemovedEvent args)
    {
        foreach (var objective in GetObjectives(ent.AsNullable()))
        {
            RaiseLocalEvent(objective, args);
        }
    }

    private void OnPlayerAttached(Entity<ESObjectiveHolderComponent> ent, ref ESMindPlayerAttachedEvent args)
    {
        foreach (var objective in GetObjectives(ent.AsNullable()))
        {
            _pvsOverride.AddSessionOverride(objective, args.Player);
        }
    }

    private void OnPlayerDetached(Entity<ESObjectiveHolderComponent> ent, ref ESMindPlayerDetachedEvent args)
    {
        foreach (var objective in GetObjectives(ent.AsNullable()))
        {
            _pvsOverride.RemoveSessionOverride(objective, args.Player);
        }
    }

    private void OnObjectiveRenamed(Entity<ESObjectiveComponent> ent, ref EntityRenamedEvent args)
    {
        // We need to dirty when we get renamed so that we can raise events on the client and update UIs.
        Dirty(ent);
    }

    /// <summary>
    /// Queries an objective to determine what its current progress is.
    /// </summary>
    public void RefreshObjectiveProgress(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return;

        var ev = new ESGetObjectiveProgressEvent();
        RaiseLocalEvent(ent, ref ev);

        var oldProgress = ent.Comp.Progress;
        var newProgress = Math.Clamp(ev.Progress, 0, 1);

        // If they are unchanged, then don't update anything.
        if (MathHelper.CloseTo(oldProgress, newProgress))
            return;

        ent.Comp.Progress = newProgress;

        var afterEv = new ESObjectiveProgressChangedEvent((ent, ent.Comp), oldProgress, newProgress);
        RaiseLocalEvent(ent, ref afterEv, true);

        Dirty(ent);
    }

    /// <summary>
    /// Refreshes objective progress for all objectives with component <see cref="T"/>
    /// </summary>
    [PublicAPI]
    public void RefreshObjectiveProgress<T>() where T : Component
    {
        foreach (var objective in GetObjectives<T>())
        {
            RefreshObjectiveProgress((objective.Owner, objective.Comp2));
        }
    }

    /// <summary>
    /// Gets the cached progress of an objective on [0, 1]
    /// If you need to update the progress, use <see cref="RefreshObjectiveProgress"/>
    /// </summary>
    public float GetProgress(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return 0;
        return ent.Comp.Progress;
    }

    /// <summary>
    /// Checks if a given objective is completed.
    /// </summary>
    public bool IsCompleted(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return false;
        return GetProgress(ent) >= 1 || MathHelper.CloseTo(GetProgress(ent), 1);
    }

    public SpriteSpecifier GetIcon(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return SpriteSpecifier.Invalid;
        return ent.Comp.Icon ?? SpriteSpecifier.Invalid;
    }

    /// <summary>
    /// Re-generates the list of objectives an entity should have, adding all new objectives and removing ones that should no longer be there,
    /// e.g. as a result of troupe or mask changes.
    /// </summary>
    public void RegenerateObjectiveList(Entity<ESObjectiveHolderComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return;

        var oldObjectives = new List<EntityUid>(ent.Comp.Objectives);
        var newObjectives = new List<EntityUid>();

        var ev = new ESGetAdditionalObjectivesEvent((ent, ent.Comp), []);
        RaiseLocalEvent(ent, ref ev);

        newObjectives.AddRange(ev.Objectives.Select(e => e.Owner));
        newObjectives.AddRange(ent.Comp.OwnedObjectives);

        var added = newObjectives.Except(oldObjectives).ToList();
        var removed = oldObjectives.Except(newObjectives).ToList();

        // Exit early if nothing has changed
        if (added.Count == 0 && removed.Count == 0)
            return;

        ent.Comp.Objectives = newObjectives;
        Dirty(ent);

        // If this holder has a player occupying it, update network status of objectives.
        // TODO: maybe make this an ESObjectivesChangedEvent sub?
        if (TryComp<MindComponent>(ent, out var mind) && _player.TryGetSessionById(mind.UserId, out var session))
        {
            foreach (var obj in added)
            {
                _pvsOverride.AddSessionOverride(obj, session);

                var addedEv = new ESObjectiveAddedEvent(ent, obj);
                RaiseLocalEvent(obj, ref addedEv);
            }
            foreach (var obj in removed)
            {
                _pvsOverride.RemoveSessionOverride(obj, session);

                var removedEv = new ESObjectiveRemovedEvent(ent, obj);
                RaiseLocalEvent(obj, ref removedEv);
            }
        }

        var changedEv = new ESObjectivesChangedEvent(newObjectives, added, removed);
        RaiseLocalEvent(ent, ref changedEv);
    }

    /// <summary>
    /// Returns all objectives on an entity
    /// </summary>
    [PublicAPI]
    public List<Entity<ESObjectiveComponent>> GetObjectives(Entity<ESObjectiveHolderComponent?> ent)
    {
        return GetObjectives<ESObjectiveComponent>(ent);
    }

    /// <summary>
    /// Returns all objectives on an entity which have a given component
    /// </summary>
    public List<Entity<T>> GetObjectives<T>(Entity<ESObjectiveHolderComponent?> ent) where T : Component
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return [];

        var objectives = new List<Entity<T>>();

        foreach (var objective in ent.Comp.Objectives)
        {
            if (!TryComp<T>(objective, out var comp))
                continue;

            objectives.Add((objective, comp));
        }

        return objectives;
    }

    /// <summary>
    /// Returns all objectives which have a given component
    /// </summary>
    [PublicAPI]
    public List<Entity<T, ESObjectiveComponent>> GetObjectives<T>() where T : Component
    {
        var query = EntityQueryEnumerator<T, ESObjectiveComponent>();

        var objectives = new List<Entity<T, ESObjectiveComponent>>();
        while (query.MoveNext(out var uid, out var comp, out var objective))
        {
            objectives.Add((uid, comp, objective));
        }

        return objectives;
    }

    /// <summary>
    /// Returns all owned objectives on an entity that have a given component
    /// </summary>
    public List<Entity<T>> GetOwnedObjectives<T>(Entity<ESObjectiveHolderComponent?> ent) where T : Component
    {
        if (!Resolve(ent, ref ent.Comp, false))
            return [];

        var objectives = new List<Entity<T>>();

        foreach (var objective in ent.Comp.OwnedObjectives)
        {
            if (!TryComp<T>(objective, out var comp))
                continue;

            objectives.Add((objective, comp));
        }

        return objectives;
    }

    /// <summary>
    /// <inheritdoc cref="CanAddObjective(Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveComponent?},Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveHolderComponent?})"/>
    /// </summary>
    [PublicAPI]
    public bool CanAddObjective(EntProtoId protoId, Entity<ESObjectiveHolderComponent?> holder)
    {
        var objectiveUid = EntityManager.PredictedSpawn(protoId, MapCoordinates.Nullspace);
        var objectiveComp = Comp<ESObjectiveComponent>(objectiveUid);
        var objectiveEnt = (objectiveUid, objectiveComp);

        var val = CanAddObjective(objectiveEnt, holder);

        // always destroy objectives created in this method.
        Del(objectiveUid);
        return val;
    }

    /// <summary>
    /// Checks if a given objective can be added
    /// </summary>
    public bool CanAddObjective(Entity<ESObjectiveComponent> ent, Entity<ESObjectiveHolderComponent?> holder)
    {
        // STUB: add events
        // TODO: the reason this isn't blocked out is because EntityTable selection (what we use objectives)
        // doesn't have real remedial behavior when deciding what objectives to select. So, at least for right now,
        // an objective failing to assign doesnt really mean anything and it just kinda results in Nothing occuring.

        return true;
    }

    /// <summary>
    /// <inheritdoc cref="TryAddObjective(Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveHolderComponent?},Robust.Shared.Prototypes.EntProtoId,out Robust.Shared.GameObjects.Entity{Content.Shared._ES.Objectives.Components.ESObjectiveComponent}?)"/>
    /// </summary>
    public bool TryAddObjective(Entity<ESObjectiveHolderComponent?> ent, EntProtoId protoId)
    {
        return TryAddObjective(ent, protoId, out _);
    }

    /// <summary>
    /// Attempts to create and add multiple objectives
    /// </summary>
    /// <returns>Returns true if all objectives succeed</returns>
    public bool TryAddObjective(Entity<ESObjectiveHolderComponent?> ent, EntityTableSelector table)
    {
        var spawns = _entityTable.GetSpawns(table);
        return spawns.All(e => TryAddObjective(ent, e));
    }

    /// <summary>
    /// Attempts to create and assign an objective to an entity
    /// </summary>
    /// <param name="ent">The entity that will be assigned the objective</param>
    /// <param name="protoId">Prototype for the objective</param>
    /// <param name="objective">The newly created objective entity</param>
    public bool TryAddObjective(
        Entity<ESObjectiveHolderComponent?> ent,
        EntProtoId protoId,
        [NotNullWhen(true)] out Entity<ESObjectiveComponent>? objective)
    {
        objective = null;

        if (!Resolve(ent, ref ent.Comp))
            return false;

        var objectiveUid = EntityManager.PredictedSpawn(protoId, MapCoordinates.Nullspace);
        var objectiveComp = Comp<ESObjectiveComponent>(objectiveUid);
        objective = (objectiveUid, objectiveComp);

        if (!CanAddObjective(objective.Value, ent))
        {
            Del(objective);
            return false;
        }

        var ev = new ESInitializeObjectiveEvent((ent, ent.Comp));
        RaiseLocalEvent(objectiveUid, ref ev);

        ent.Comp.OwnedObjectives.Add(objective.Value);
        RegenerateObjectiveList(ent);
        RefreshObjectiveProgress(objective.Value.AsNullable());
        return true;
    }

    public bool TryRemoveObjective(Entity<ESObjectiveHolderComponent?> ent, Entity<ESObjectiveComponent?> objective)
    {
        if (!Resolve(ent, ref ent.Comp) || !Resolve(objective, ref objective.Comp))
            return false;

        ent.Comp.OwnedObjectives.Remove(objective);
        RegenerateObjectiveList(ent);
        Del(objective);
        return true;
    }

    /// <summary>
    ///     Tries to find an <see cref="ESObjectiveHolderComponent"/>
    ///     This is kind of inefficient, so you should avoid doing this when possible (and try to just pass around the holder separately, if you already know it)
    /// </summary>
    public bool TryFindObjectiveHolder(Entity<ESObjectiveComponent?> objective, [NotNullWhen(true)] out Entity<ESObjectiveHolderComponent>? holder)
    {
        if (!Resolve(objective.Owner, ref objective.Comp))
        {
            holder = null;
            return false;
        }

        Entity<ESObjectiveHolderComponent>? foundHolder = null;
        var query = EntityQueryEnumerator<ESObjectiveHolderComponent>();
        while (query.MoveNext(out var uid, out var potentialHolder))
        {
            if (!potentialHolder.OwnedObjectives.Contains(objective.Owner))
                continue;

            // this assumes only one holder can own an objective, which I think is true right now, and the purpose of owned objectives anyway?
            foundHolder = (uid, potentialHolder);
            break;
        }

        holder = foundHolder;
        return holder != null;
    }

    /// <summary>
    /// Checks if a given entity has the given objective assigned to them.
    /// Unlike <see cref="TryFindObjectiveHolder"/>, this works for any type of inherited ownership, not just direct holding.
    /// </summary>
    public bool HasObjective<T>(EntityUid potentialHolder, Entity<T> objective) where T : Component
    {
        return GetObjectives<T>(potentialHolder).Contains(objective);
    }

    public string GetObjectiveString(Entity<ESObjectiveComponent?> ent)
    {
        if (!Resolve(ent, ref ent.Comp))
            return string.Empty;

        return Loc.GetString("es-objective-summary-fmt",
            ("name", Name(ent)),
            ("success", IsCompleted(ent)),
            ("percent", (int) (GetProgress(ent) * 100)));
    }
}
