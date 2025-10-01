using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.EntityTable.EntitySelectors;

/// <summary>
/// See <see cref="NestedSelector"/>
/// </summary>
public sealed partial class ESNestedSelector : EntityTableSelector
{
    public const string DataFieldTag = "tableId";

    [DataField(DataFieldTag, required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return proto.Index(TableId).Table.GetSpawns(rand, entMan, proto, ctx);
    }
}
