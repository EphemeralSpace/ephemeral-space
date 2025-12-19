using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;

namespace Content.Server._ES.Masks.Objectives;

public sealed class ESEatUniqueFoodsObjectiveSystem : ESBaseObjectiveSystem<ESEatUniqueFoodsObjectiveComponent>
{
    public override Type[] RelayComponents => [typeof(ESMuncherRelayComponent)];

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
        SubscribeLocalEvent<ESEatUniqueFoodsObjectiveComponent, ESBodyFullyAteEvent>(OnBodyFullyAte);
    }

    private void OnBodyFullyAte(Entity<ESEatUniqueFoodsObjectiveComponent> ent, ref ESBodyFullyAteEvent args)
    {
        // Yes, prototype matching sucks ass.
        // Cooking in general is abysmally bad and I'd really rather
        // do anything else but this. But, we glutton on.
        if (Prototype(args.Food) is not { } prototype)
            return;

        ent.Comp.UniqueFoods.Add(prototype);
        ObjectivesSys.SetObjectiveCounter(ent.Owner, ent.Comp.UniqueFoods.Count);
    }
}
