using System.Linq;
using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESEatFoodObjectiveSystem : ESBaseObjectiveSystem<ESEatFoodObjectiveComponent>
{
    [Dependency] private readonly IPrototypeManager _prototype = default!;
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly MetaDataSystem _metaData = default!;

    public override Type[] RelayComponents => [typeof(ESMuncherRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESEatFoodObjectiveComponent, ESBodyFullyAteEvent>(OnBodyFullyAte);
    }

    protected override void InitializeObjective(Entity<ESEatFoodObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        var otherFoods = ObjectivesSys.GetObjectives<ESEatFoodObjectiveComponent>()
            .Where(e => e.Comp1.SelectedFood.HasValue)
            .Select(e => e.Comp1.SelectedFood!.Value);

        var foodChoices = ent.Comp.Foods.Except(otherFoods).ToList();
        if (foodChoices.Count == 0)
            return;

        ent.Comp.SelectedFood = _random.Pick(foodChoices);
        var foodName = _prototype.Index(ent.Comp.SelectedFood.Value).Name;
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("name", foodName)));
    }

    private void OnBodyFullyAte(Entity<ESEatFoodObjectiveComponent> ent, ref ESBodyFullyAteEvent args)
    {
        if (Prototype(args.Food)?.ID == ent.Comp.SelectedFood)
            ObjectivesSys.AdjustObjectiveCounter(ent.Owner);
    }
}
