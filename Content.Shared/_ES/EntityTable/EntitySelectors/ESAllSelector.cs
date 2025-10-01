using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.EntityTable.EntitySelectors;

/// <summary>
/// See <see cref="AllSelector"/>
/// </summary>
public sealed partial class ESAllSelector : EntityTableSelector
{
    public const string DataFieldTag = "all";

    [DataField(DataFieldTag, required: true)]
    public List<EntityTableSelector> Children;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
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
