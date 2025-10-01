using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.EntityTable.EntitySelectors;

/// <summary>
/// See <see cref="GroupSelector"/>
/// </summary>
public sealed partial class ESGroupSelector : EntityTableSelector
{
    public const string DataFieldTag = "group";

    [DataField(DataFieldTag, required: true)]
    public List<EntityTableSelector> Children = new();

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var children = new Dictionary<EntityTableSelector, float>(Children.Count);
        foreach (var child in Children)
        {
            // Don't include invalid groups
            if (!child.CheckConditions(entMan, proto, ctx))
                continue;

            children.Add(child, child.Weight);
        }

        if (children.Count == 0)
            return Array.Empty<EntProtoId>();

        var pick = SharedRandomExtensions.Pick(children, rand);

        return pick.GetSpawns(rand, entMan, proto, ctx);
    }
}
