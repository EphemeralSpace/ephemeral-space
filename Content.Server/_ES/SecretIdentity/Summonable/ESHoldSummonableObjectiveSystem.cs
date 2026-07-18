using Content.Server._ES.SecretIdentity.Summonable.Components;
using Content.Shared._ES.SecretIdentity.Summonable.Components;
using Content.Shared._ES.Objectives;
using Robust.Server.Containers;
using Robust.Shared.Containers;

namespace Content.Server._ES.SecretIdentity.Summonable;

/// <summary>
/// This handles <see cref="ESHoldSummonableObjectiveComponent"/>
/// </summary>
public sealed partial class ESHoldSummonableObjectiveSystem : ESBaseObjectiveSystem<ESHoldSummonableObjectiveComponent>
{
    [Dependency] private ContainerSystem _container = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSecretIdentitySummonedComponent, EntGotInsertedIntoContainerMessage>(OnGotInserted);
        SubscribeLocalEvent<ESSecretIdentitySummonedComponent, EntGotRemovedFromContainerMessage>(OnGotRemoved);
    }

    private void OnGotInserted(Entity<ESSecretIdentitySummonedComponent> ent, ref EntGotInsertedIntoContainerMessage args)
    {
        if (ent.Comp.OwnerMind is not { } mind)
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESHoldSummonableObjectiveComponent>(mind))
        {
            RefreshHeldCount(objective);
        }
    }

    private void OnGotRemoved(Entity<ESSecretIdentitySummonedComponent> ent, ref EntGotRemovedFromContainerMessage args)
    {
        if (ent.Comp.OwnerMind is not { } mind)
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESHoldSummonableObjectiveComponent>(mind))
        {
            RefreshHeldCount(objective);
        }
    }

    private void RefreshHeldCount(Entity<ESHoldSummonableObjectiveComponent> ent)
    {
        var held = 0;

        var query = EntityQueryEnumerator<ESSecretIdentitySummonedComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var summoned, out var xform))
        {
            if (summoned.OwnerMind == null)
                continue;

            if (!ObjectivesSys.GetObjectives<ESHoldSummonableObjectiveComponent>(summoned.OwnerMind.Value).Contains(ent))
                continue;

            if (IsHeld((uid, xform), ent))
                held += 1;
        }

        ObjectivesSys.SetObjectiveCounter(ent.Owner, held);
    }

    private bool IsHeld(Entity<TransformComponent> ent, Entity<ESHoldSummonableObjectiveComponent> objective)
    {
        foreach (var container in _container.GetContainingContainers(ent.AsNullable()))
        {
            if (MindSys.TryGetMind(container.Owner, out var containerMind, out _) &&
                !ObjectivesSys.GetObjectives<ESHoldSummonableObjectiveComponent>(containerMind).Contains(objective))
                return true;
        }

        return false;
    }
}
