using Content.Server._ES.SecretIdentity.Hemophage.Components;
using Content.Server._ES.SecretIdentity.Parasite;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Chemistry.Reagent;
using Content.Shared.FixedPoint;
using Content.Shared.Fluids.Components;
using Content.Shared.Forensics.Components;
using Content.Shared.Mind;

namespace Content.Server._ES.SecretIdentity.Hemophage;

public sealed partial class ESHemophageSystem : ESBaseParasiteSystem<ESHemophageComponent>
{
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    protected override void OnValidParasiteKill(Entity<ESHemophageComponent> ent,
        EntityUid killed,
        EntityUid killer,
        Entity<MindComponent> killedMind,
        Entity<MindComponent> killerMind)
    {
        if (!TryComp<DnaComponent>(killed, out var dna) || dna.DNA == null)
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
