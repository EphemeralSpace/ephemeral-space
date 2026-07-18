using Content.Server._ES.SecretIdentity.Summonable.Components;
using Content.Server.Mind;
using Content.Shared._ES.SecretIdentity.Summonable.Components;
using Content.Shared._ES.Objectives;
using Content.Shared.Nutrition;

namespace Content.Server._ES.SecretIdentity.Summonable;

/// <summary>
/// This handles <see cref="ESEatSummonableObjectiveComponent"/>
/// </summary>
public sealed class ESEatSummonableObjectiveSystem : ESBaseObjectiveSystem<ESEatSummonableObjectiveComponent>
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESSecretIdentitySummonedComponent, FullyEatenEvent>(OnFullyEaten);
    }

    private void OnFullyEaten(Entity<ESSecretIdentitySummonedComponent> ent, ref FullyEatenEvent args)
    {
        if (Deleted(ent.Comp.OwnerMind))
            return;

        if (!MindSys.TryGetMind(args.User, out var mind, out _) ||
            mind == ent.Comp.OwnerMind)
            return;

        foreach (var objective in ObjectivesSys.GetObjectives<ESEatSummonableObjectiveComponent>(ent.Comp.OwnerMind.Value))
        {
            ObjectivesSys.AdjustObjectiveCounter(objective.Owner);
        }
    }
}
