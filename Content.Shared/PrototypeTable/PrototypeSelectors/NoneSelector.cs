using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

/// <summary>
/// Selects nothing.
/// </summary>
public sealed partial class NoneSelector<T> : PrototypeTableSelector<T>
    where T: class, IPrototype
{
    protected override IEnumerable<ProtoId<T>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        yield break;
    }
}
