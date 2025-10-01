using System.Linq;
using Content.Shared.EntityTable;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;

namespace Content.Shared._ES.EntityTable.EntitySelectors;

/// <summary>
/// Picks out a specified number of spawns out of all spawns provided by child selectors.
/// Does not respect weight.
/// </summary>
/// <remarks>
/// This is essentially the same as <see cref="GroupSelector"/> except it selects a single spawn instead of a single selector.
/// </remarks>
public sealed partial class ESPickSelector : EntityTableSelector
{
    public const string DataFieldTag = "pick";

    [DataField(DataFieldTag, required: true)]
    public EntityTableSelector Child;

    protected override IEnumerable<EntProtoId> GetSpawnsImplementation(System.Random rand,
        IEntityManager entMan,
        IPrototypeManager proto,
        EntityTableContext ctx)
    {
        var pool = Child.GetSpawns(rand, entMan, proto, ctx).ToList();
        yield return rand.Pick(pool);
    }
}
