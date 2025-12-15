using Content.Server.Database.Migrations.Sqlite;
using Content.Server.Popups;
using Content.Shared.Chemistry.Components;
using Content.Shared.Chemistry.Components.SolutionManager;
using Content.Shared.Chemistry.EntitySystems;
using Content.Shared.Mobs.Components;
using Content.Shared.Mobs.Systems;
using Content.Shared.Popups;

namespace Content.Server._ES.Masks.Chemicalnjection;

public sealed class ChemicalInjectorSystem : EntitySystem
{

    [Dependency] protected readonly SharedSolutionContainerSystem _SolutionContainer = default!;
    [Dependency] protected readonly MobStateSystem _mobState = default!;
    [Dependency] protected readonly PopupSystem _popupSystem = default!;

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
            _popupSystem.PopupEntity(Loc.GetString(args.NotCrit), args.Performer, args.Performer, PopupType.Medium);
            return;
        }

        if (!TryComp<SolutionContainerManagerComponent>(args.Action.Owner, out var solutionManager))
            return;

        if (!TryComp<InjectableSolutionComponent>(args.Performer, out var injectableSolutionComponent))
            return;

        _SolutionContainer.TryGetSolution((args.Action.Owner, solutionManager), args.SolutionName, out var entity, out var solution);

        _SolutionContainer.TryGetInjectableSolution((args.Performer, injectableSolutionComponent), out var entitysolution, out var injectableSolution);

        if (solution == null)
            return;

        _SolutionContainer.AddSolution((Entity<SolutionComponent>)entitysolution!, solution);

        args.Handled = true;
    }

}
