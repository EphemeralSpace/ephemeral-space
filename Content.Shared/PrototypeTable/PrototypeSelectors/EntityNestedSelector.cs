using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

/// <summary>
/// Gets the spawns from the entity table prototype specified.
/// Can be used to reuse common tables.
/// </summary>
public sealed partial class EntityNestedSelector : PrototypeTableSelector<EntityPrototype>
{
    [DataField(required: true)]
    public ProtoId<EntityTablePrototype> TableId;

    protected override IEnumerable<ProtoId<EntityPrototype>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        return proto.Index(TableId).Table.GetSpawns(rand, entMan, proto, ctx);
    }
}
