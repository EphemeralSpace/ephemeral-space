using Content.Shared.PrototypeTable.ValueSelector;
using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable.PrototypeSelectors;

/// <summary>
/// Gets the spawn for the specified prototype ID at whatever count specified.
/// </summary>
public sealed partial class IdSelector<T> : PrototypeTableSelector<T>
    where T: class, IPrototype
{
    public const string IdDataFieldTag = "id";

    [DataField(IdDataFieldTag, required: true)]
    public ProtoId<T> Id;

    [DataField]
    public NumberSelector Amount = new ConstantNumberSelector(1);

    protected override IEnumerable<ProtoId<T>> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var num = Amount.Get(rand);
        for (var i = 0; i < num; i++)
        {
            yield return Id;
        }
    }
}
