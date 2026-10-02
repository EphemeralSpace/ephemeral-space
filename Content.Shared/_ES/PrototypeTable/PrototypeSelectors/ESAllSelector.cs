using Content.Shared.PrototypeTable;
using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.PrototypeTable.PrototypeSelectors;

/// <summary>
/// See <see cref="AllSelector{T}"/>
/// </summary>
public sealed partial class ESAllSelector<T> : PrototypeTableSelector<T>
    where T: class, IPrototype
{
    public const string DataFieldTag = "all";

    [DataField(DataFieldTag, required: true)]
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
