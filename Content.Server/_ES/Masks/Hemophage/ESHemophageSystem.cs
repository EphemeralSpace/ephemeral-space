using Content.Server._ES.Masks.Hemophage.Components;
using Content.Shared._ES.KillTracking.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Mind;

namespace Content.Server._ES.Masks.Hemophage;

public sealed class ESHemophageSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESHemophageComponent, ESPlayerKilledEvent>(OnPlayerKilled);
    }

    private void OnPlayerKilled(Entity<ESHemophageComponent> ent, ref ESPlayerKilledEvent args)
    {
        if (!TryComp<MindComponent>(ent, out var mind) ||
            mind.OwnedEntity is not { } owned)
            return;

        if (!TryComp<DnaComponent>(owned, out var dna) || dna.DNA == null)
            return;

        var query = EntityQueryEnumerator<PuddleComponent, SolutionContainerManagerComponent, TransformComponent>();
        while (query.MoveNext(out var uid, out var puddle, out var solution, out var xform))
        {
            if (!_solutionContainer.TryGetSolution((uid, solution), puddle.SolutionName, out _, out var puddleSolution))
                continue;

            var dnaTotal = FixedPoint2.Zero;
            foreach (var reagent in puddleSolution.Contents)
            {
                foreach (var data in reagent.Reagent.EnsureReagentData())
                {
                    if (data is not DnaData dnaData)
                        continue;

                    if (dnaData.DNA != dna.DNA)
                        continue;

                    dnaTotal += reagent.Quantity;
                    break;
                }
            }

            if (dnaTotal > ent.Comp.BloodThreshold)
            {
                SpawnAtPosition(ent.Comp.PuddleSpawn, xform.Coordinates);
            }
        }
    }
}
