using System.Linq;
using Content.Shared._ES.Objectives.Target.Components;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Objectives.Target;

public sealed partial class ESTargetCodenameSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;
    [Dependency] private ESSharedObjectiveSystem _objective = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        SubscribeLocalEvent<ESTargetCodenameComponent, ESObjectiveTargetChangedEvent>(GetCodename);
    }

    private void GetCodename(Entity<ESTargetCodenameComponent> ent, ref ESObjectiveTargetChangedEvent args)
    {
        if (args.NewTarget == null)
            return;

        var usedCodenames = _objective.GetObjectives<ESTargetCodenameComponent>()
            .Where(o => o.Comp1.Codename.HasValue)
            .Select(o => (string) o.Comp1.Codename!.Value);

        var codenames = new List<string>(_prototype.Index(ent.Comp.CodenameDataset).Values)
            .Except(usedCodenames)
            .ToList();

        var codename = _random.PickAndTake(codenames);
        ent.Comp.Codename = codename;

        if (ent.Comp.Title is not null)
        {
            var title = Loc.GetString(ent.Comp.Title, ("codename", Loc.GetString(codename)));
            _metaData.SetEntityName(ent, title);
        }
    }
}
