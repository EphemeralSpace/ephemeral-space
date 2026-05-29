using Content.Shared._ES.Masks.Traitor.Components;
using Content.Shared._ES.Objectives;
using Content.Shared._ES.Objectives.Components;
using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.Masks.Traitor;

public sealed partial class ESTraitorBugSystem : ESBaseObjectiveSystem<ESTraitorBugObjectiveComponent>
{
    [Dependency] private IPrototypeManager _prototype = default!;
    [Dependency] private IRobustRandom _random = default!;
    [Dependency] private MetaDataSystem _metaData = default!;

    // TODO: This is mostly just a bad hack for the fact that you can't have a nullable value that's easily editable in VV.
    // I would like to not go insane editing all the APCs for this until we have an actual system that does it semi-reasonably.
    private static readonly ProtoId<AccessGroupPrototype> IgnoreDepartment = "AllAccess";

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }

    protected override void InitializeObjective(Entity<ESTraitorBugObjectiveComponent> ent, ref ESInitializeObjectiveEvent args)
    {
        base.InitializeObjective(ent, ref args);

        var options = new HashSet<ProtoId<AccessGroupPrototype>>();

        var query = EntityQueryEnumerator<ESTraitorBuggableComponent>();
        while (query.MoveNext(out var comp))
        {
            if (comp.Department != IgnoreDepartment)
                options.Add(comp.Department);
        }

        if (options.Count == 0)
            return;

        var accessGroup = _prototype.Index(_random.Pick(options));

        ent.Comp.Target = accessGroup;
        _metaData.SetEntityName(ent, Loc.GetString(ent.Comp.Title, ("department", accessGroup.GetAccessGroupName())));
        Dirty(ent);
    }
}
