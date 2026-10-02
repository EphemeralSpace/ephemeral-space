using Content.Shared.Random.Helpers;
using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

/// <summary>
/// Gets the spawns from one of the child selectors, based on the weight of the children
/// </summary>
public sealed partial class GroupSelector<T> : PrototypeTableSelector<T>
    where T: class, IPrototype
{
    public const string DataFieldTag = "group";

    [DataField(required: true)]
    public List<PrototypeTableSelector<T>> Children = new();

    protected override IEnumerable<ProtoId<T>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var children = new Dictionary<PrototypeTableSelector<T>, float>(Children.Count);
        foreach (var child in Children)
        {
            // Don't include invalid groups
            if (!child.CheckConditions(entMan, proto, ctx))
                continue;

            children.Add(child, child.Weight);
        }

        if (children.Count == 0)
            return Array.Empty<ProtoId<T>>();

        var pick = SharedRandomExtensions.Pick(children, rand);

        return pick.GetSpawns(rand, entMan, proto, ctx);
    }
}
