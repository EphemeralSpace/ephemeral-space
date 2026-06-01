using Content.Shared.Nutrition;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Shared._ES.Food;

/// <see cref="ESFoodComponent"/>
public sealed partial class ESFoodSystem : EntitySystem
{
    [Dependency] private HungerSystem _hunger = default!;

    public override void Initialize()
    {
        SubscribeLocalEvent<ESFoodComponent, BeforeIngestedEvent>(OnBeforeFoodIngested, after: [typeof(IngestionSystem)]);
        SubscribeLocalEvent<ESFoodComponent, IngestedEvent>(OnFoodIngested, after: [typeof(IngestionSystem)]);
    }

    private void OnBeforeFoodIngested(Entity<ESFoodComponent> ent, ref BeforeIngestedEvent args)
    {
        // shouldnt be possible but cancel anyway
        if (ent.Comp.PortionsLeft <= 0)
        {
            args.Cancelled = true;
            return;
        }

        // no problem, we are fine with no solution existing
        if (args.Solution == null)
            return;

        args.Transfer = args.Solution.Volume;
        // would you look at that this existed already
        args.Refresh = true;
    }

    private void OnFoodIngested(Entity<ESFoodComponent> ent, ref IngestedEvent args)
    {
        ent.Comp.PortionsLeft ??= ent.Comp.StartingPortions;
        ent.Comp.PortionsLeft -= 1;

        // we run after ingestion and so will override its behavior for destroying
        args.Destroy = ent.Comp.PortionsLeft <= 0;

        // satiate
        _hunger.ModifySatiety(args.User, 1 * ent.Comp.SatietyMultiplier);
        Dirty(ent);
    }
}
