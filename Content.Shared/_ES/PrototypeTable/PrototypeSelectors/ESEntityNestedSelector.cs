using Content.Shared.PrototypeTable;
using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.PrototypeTable.PrototypeSelectors;

/// <summary>
/// See <see cref="EntityNestedSelector"/>
/// </summary>
public sealed partial class ESEntityNestedSelector : PrototypeTableSelector<EntityPrototype>
{
    public const string DataFieldTag = "tableId";

    [DataField(DataFieldTag, required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    protected override IEnumerable<ProtoId<EntityPrototype>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return proto.Index(TableId).Table.GetSpawns(rand, entMan, proto, ctx);
    }
}
