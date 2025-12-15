using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Shared._ES.Masks.ChemicalInjector;

public sealed class ChemicalInjectorSystem : EntitySystem
{
    [Dependency] private readonly SharedSolutionContainerSystem _solutionContainer = default!;
    [Dependency] private readonly MobStateSystem _mobState = default!;
    [Dependency] private readonly SharedPopupSystem _popupSystem = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESChemicalInjectorEvent>(OnChemicalInjection);
    }

    private void OnChemicalInjection(ESChemicalInjectorEvent args)
    {
        if (args.Handled)
            return;

        if (!_mobState.IsCritical(args.Performer) && args.OnlyUsableWhileCrit)
        {
            _popupSystem.PopupPredicted(Loc.GetString(args.FailMessage), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!_solutionContainer.TryGetSolution(args.Action.Owner, args.SolutionName, out _, out var solution) ||
            !_solutionContainer.TryGetInjectableSolution(args.Performer, out var targetSolution, out _))
            return;

        _solutionContainer.AddSolution(targetSolution.Value, solution);

        args.Handled = true;
    }
}
