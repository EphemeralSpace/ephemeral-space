using Content.Server._ES.SecretIdentity.Objectives.Components;
using Content.Server._ES.SecretIdentity.Objectives.Relays;
using Content.Server._ES.SecretIdentity.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.SecretIdentity.Objectives;

/// <summary>
///     This handles the imbibe unique reagents objective, for consuming N reagents.
/// </summary>
/// <seealso cref="ESImbibeUniqueReagentsObjectiveComponent"/>
public sealed class ESImbibeUniqueReagentsObjectiveSystem : ESBaseObjectiveSystem<ESImbibeUniqueReagentsObjectiveComponent>
{
    public override Type[] RelayComponents => new[] { typeof(ESMuncherRelayComponent) };

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESImbibeUniqueReagentsObjectiveComponent, ESBodyIngestingEvent>(OnBodyIngesting);
    }

    private void OnBodyIngesting(Entity<ESImbibeUniqueReagentsObjectiveComponent> ent, ref ESBodyIngestingEvent args)
    {
        if (!ent.Comp.CanBeFromFood && !args.IsDrink)
            return;

        if (args.FoodSolution == null)
            return;

        foreach (var reagent in args.FoodSolution)
        {
            ent.Comp.SeenReagents.Add(reagent.Reagent);
        }

        ObjectivesSys.SetObjectiveCounter(ent.Owner, ent.Comp.SeenReagents.Count);
    }
}
