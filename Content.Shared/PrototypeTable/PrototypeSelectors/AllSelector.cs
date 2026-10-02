using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

/// <summary>
/// Gets spawns from all of the child selectors
/// </summary>
public sealed partial class AllSelector<T> : PrototypeTableSelector<T>
    where T: class, IPrototype
{
    [DataField(required: true)]
    public List<PrototypeTableSelector<T>> Children;

    protected override IEnumerable<ProtoId<T>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        foreach (var child in Children)
        {
            foreach (var spawn in child.GetSpawns(rand, entMan, proto, ctx))
            {
                yield return spawn;
            }
        }
    }
}
