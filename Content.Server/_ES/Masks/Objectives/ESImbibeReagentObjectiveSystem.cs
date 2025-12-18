using System.Linq;
using Content.Server._ES.Masks.Objectives.Components;
using Content.Server._ES.Masks.Objectives.Relays;
using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Objectives;

/// <summary>
///     This handles the imbibe reagent objective, for consuming a specific reagent.
/// </summary>
/// <seealso cref="ESImbibeReagentObjectiveComponent"/>
public sealed class ESImbibeReagentObjectiveSystem : ESBaseObjectiveSystem<ESImbibeReagentObjectiveComponent>
{
    [Dependency] private readonly IRobustRandom _random = default!;
    [Dependency] private readonly IPrototypeManager _proto = default!;
    [Dependency] private readonly MetaDataSystem _meta = default!;

    public override Type[] RelayComponents => new[] { typeof(ESMuncherRelayComponent) };

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESImbibeReagentObjectiveComponent, ESBodyIngestingEvent>(OnBodyIngesting);
    }

    protected override void InitializeObjective(Entity<ESImbibeReagentObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        ent.Comp.ConsumeTarget = _random.Pick(ent.Comp.PossibleConsumeTargets);

        var reagentName = _proto.Index(ent.Comp.ConsumeTarget).LocalizedName;

        _meta.SetEntityDescription(ent, Loc.GetString(ent.Comp.DescriptionLoc, ("reagent", reagentName)));
        _meta.SetEntityName(ent,
            TryComp<ESCounterObjectiveComponent>(ent, out var counter)
                ? Loc.GetString(ent.Comp.TitleLoc, ("reagent", reagentName), ("count", counter.Target))
                : Loc.GetString(ent.Comp.TitleLoc, ("reagent", reagentName)));
    }

    private void OnBodyIngesting(Entity<ESImbibeReagentObjectiveComponent> ent, ref ESBodyIngestingEvent args)
    {
        if (!args.IsDrink)
            return;

        // I solemnly swear this is the best way I found to do this. Weird ass API.
        var reagents = args.FoodSolution.Contents
            .Where(x => x.Reagent.Prototype == ent.Comp.ConsumeTarget)
            .Select(x => x.Quantity)
            .ToList();

        // can't use Sum() on FixedPoint2, and Aggregate yells about empty lists.
        if (reagents.Count > 0)
            ObjectivesSys.AdjustObjectiveCounter(ent.Owner, reagents.Aggregate(static (x, y) => x + y).Float());
    }
}
