using Content.Server._ES.Masks.Objectives.Relays.Components;
using Content.Server.Mind;
using Content.Shared._ES.Mind;
using Content.Shared.Chemistry.Components;
using Content.Shared.Nutrition;
using Content.Shared.Nutrition.Components;
using Content.Shared.Nutrition.EntitySystems;

namespace Content.Server._ES.Masks.Objectives.Relays;

/// <summary>
///     This handles relaying <see cref="IngestingEvent"/> to the mind, allowing other objectives to listen to it.
///     It also contains some best effort logic to decipher if something is food or drink.
/// </summary>
public sealed class ESMuncherRelaySystem : ESBaseMindRelay
{
    [Dependency] private readonly MindSystem _mind = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESMuncherRelayComponent, IngestingEvent>(OnIngesting);
        SubscribeLocalEvent<ESMuncherRelayComponent, FullyAteEvent>(OnFullyAte);
    }

    private void OnFullyAte(Entity<ESMuncherRelayComponent> ent, ref FullyAteEvent args)
    {
        // TODO(Kaylie): Mind doesn't have an Entity<T> override for this. For reasons.
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        var ev = new ESBodyFullyAteEvent(ent, args.Food);

        RaiseMindEvent((mindId, mindComp), ref ev);
    }

    private void OnIngesting(Entity<ESMuncherRelayComponent> ent, ref IngestingEvent args)
    {
        if (!_mind.TryGetMind(ent, out var mindId, out var mindComp))
            return;

        //TODO(Kaylie): Ew, protoid comparison. Nothing better, though.
        var isFood = TryComp<EdibleComponent>(ent, out var edible) && edible.Edible != IngestionSystem.Drink;

        var ev = new ESBodyIngestingEvent(ent, args.Food, args.Split, args.ForceFed, !isFood);

        RaiseMindEvent((mindId, mindComp), ref ev);
    }
}

/// <summary>
///     Raised directed on the mind when the body has ingested something.
/// </summary>
/// <param name="Body">The body in question.</param>
/// <param name="Food">The food/drink in question.</param>
/// <param name="FoodSolution">The solution contents of that food.</param>
/// <param name="IsForceFed">Whether we're being forcefed.</param>
/// <param name="IsDrink">Whether this is a drink.</param>
[ByRefEvent]
public readonly record struct ESBodyIngestingEvent(EntityUid Body, EntityUid Food, Solution FoodSolution, bool IsForceFed, bool IsDrink);

/// <summary>
///     Raised directed on the mind when the body has fully consumed some food and it's about to be deleted.
/// </summary>
/// <param name="Body">The body in question.</param>
/// <param name="Food">The food item in question. Never a drink.</param>
[ByRefEvent]
public readonly record struct ESBodyFullyAteEvent(EntityUid Body, EntityUid Food);
