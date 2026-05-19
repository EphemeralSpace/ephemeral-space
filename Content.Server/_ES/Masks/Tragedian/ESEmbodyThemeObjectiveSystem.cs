using Content.Server._ES.Masks.Tragedian.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Server._ES.Masks.Tragedian;

public sealed partial class ESEmbodyThemeObjectiveSystem : ESBaseObjectiveSystem<ESEmbodyThemeObjectiveComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void InitializeObjective(Entity<ESEmbodyThemeObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        var dataset = _prototype.Index(ent.Comp.ThemeDataset);
        ent.Comp.Theme = _random.Pick(dataset);

        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("theme", ent.Comp.Theme)));
    }
}
