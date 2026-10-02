using System.Linq;
using Content.Shared.PrototypeTable;
using Content.Shared.PrototypeTable.PrototypeSelectors;
using Content.Shared.PrototypeTable.ValueSelector;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.PrototypeTable.PrototypeSelectors;

/// <summary>
/// Picks out a specified number of spawns out of all spawns provided by child selectors.
/// Does not respect weight.
/// </summary>
/// <remarks>
/// This is essentially the same as <see cref="GroupSelector{T}"/> except it selects a single spawn instead of a single selector.
/// </remarks>
public sealed partial class ESPickSelector<T> : PrototypeTableSelector<T>
    where T: class, IPrototype
{
    public const string DataFieldTag = "pick";

    [DataField(DataFieldTag, required: true)]
    public PrototypeTableSelector<T> Child;

    [DataField]
    public NumberSelector Amount = new ConstantNumberSelector(1);

    protected override IEnumerable<ProtoId<T>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        // This is mildly evil but uh...
        // We violate spec sometimes.
        var r = new RobustRandom();
        var pool = Child.GetSpawns(rand, entMan, proto, ctx).ToArray();
        return r.GetItems(pool, Amount.Get(rand), allowDuplicates: false);
    }
}
