using Content.Shared.Chemistry.EntitySystems;

namespace Content.Shared._ES.Changeling;

public sealed class ChemicalStingSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChemicalStingEvent>(OnChemicalInjection);
    }

    private void OnChemicalInjection(ESChemicalStingEvent args)
    {
        if (args.Handled)
            return;

        if (!_solutionContainer.TryGetSolution(args.Action.Owner, args.SolutionName, out _, out var solution) ||
            !_solutionContainer.TryGetInjectableSolution(args.Target, out var targetSolution, out _))
            return;

        _solutionContainer.AddSolution(targetSolution.Value, solution);

        args.Handled = true;
    }
}
