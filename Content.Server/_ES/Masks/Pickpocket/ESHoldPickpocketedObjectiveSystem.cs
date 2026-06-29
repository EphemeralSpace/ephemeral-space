using System.Linq;
using Content.Server._ES.Masks.Pickpocket.Components;
using Content.Shared._ES.Objectives;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._ES.Masks.Pickpocket;

public sealed partial class ESHoldPickpocketedObjectiveSystem : ESBaseObjectiveSystem<ESHoldPickpocketedObjectiveComponent>
{
    [Dependency] private ContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESPickpocketStolenComponent, EntGotInsertedIntoContainerMessage>(OnGotInserted);
        SubscribeLocalEvent<ESPickpocketStolenComponent, EntGotRemovedFromContainerMessage>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<ESPickpocketStolenComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        foreach (var mind in ent.Comp.StealerMinds)
        {
            foreach (var objective in ObjectivesSys.GetObjectives<ESHoldPickpocketedObjectiveComponent>(mind))
            {
                RefreshHeldCount(objective);
            }
        }
    }

    private void OnGotRemoved(Entity<ESPickpocketStolenComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        foreach (var mind in ent.Comp.StealerMinds)
        {
            foreach (var objective in ObjectivesSys.GetObjectives<ESHoldPickpocketedObjectiveComponent>(mind))
            {
                RefreshHeldCount(objective);
            }
        }
    }

    private void RefreshHeldCount(Entity<ESHoldPickpocketedObjectiveComponent> ent)
    {
        var stolenFrom = new HashSet<EntityUid>();

        var query = EntityQueryEnumerator<ESPickpocketStolenComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var stolen, out var xform))
        {
            if (!IsHeld((uid, stolen, xform)))
                continue;

            var uniqueStolen = stolen.StolenMinds.Except(stolenFrom).ToList();
            if (uniqueStolen.Count != 0)
                stolenFrom.Add(uniqueStolen.First());
        }

        ObjectivesSys.SetObjectiveCounter(ent.Owner, stolenFrom.Count);
    }

    private bool IsHeld(Entity<ESPickpocketStolenComponent, TransformComponent> ent)
    {
        foreach (var container in _container.GetContainingContainers((ent, ent.Comp2)))
        {
            if (MindSys.TryGetMind(container.Owner, out var containerMind, out _) &&
                ent.Comp1.StealerMinds.Contains(containerMind))
                return true;
        }

        return false;
    }
}
