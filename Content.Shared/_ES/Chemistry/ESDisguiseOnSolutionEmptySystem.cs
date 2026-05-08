using Content.Shared._ES.Chemistry.Components;
using Content.Shared.Chemistry.EntitySystems;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chemistry;

public sealed partial class ESDisguiseOnSolutionEmptySystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private SharedSolutionContainerSystem _solutionContainer = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESDisguiseOnSolutionEmptyComponent, SolutionContainerChangedEvent>(OnSolutionChange);
    }

    private void OnSolutionChange(Entity<ESDisguiseOnSolutionEmptyComponent> ent, ref SolutionContainerChangedEvent args)
    {
        if (!_solutionContainer.TryGetSolution(ent.Owner, ent.Comp.Solution, out _, out var solution))
            return;

        if (solution.Volume <= 0)
        {
            var disguise = _prototypeManager.Index(ent.Comp.Disguise);

            _metaData.SetEntityName(ent, disguise.Name);
            _metaData.SetEntityDescription(ent, disguise.Description);
            return;
        }

        var proto = Prototype(ent.Owner);
        _metaData.SetEntityName(ent, proto?.Name ?? string.Empty);
        _metaData.SetEntityDescription(ent, proto?.Description ?? string.Empty);
    }
}
