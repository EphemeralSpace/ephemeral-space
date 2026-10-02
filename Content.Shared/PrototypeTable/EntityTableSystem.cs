using System.Diagnostics.CodeAnalysis;
using System.Linq;
using Content.Shared.PrototypeTable.PrototypeSelectors;
using JetBrains.Annotations;
using Robust.Shared.Prototypes;
using Robust.Shared.Random;
using Robust.Shared.Utility;

namespace Content.Shared.PrototypeTable;

public sealed partial class EntityTableSystem : EntitySystem
{
    [Dependency] private IPrototypeManager _prototypeManager = default!;
    [Dependency] private IRobustRandom _random = default!;

    public EntProtoId GetSingleSpawn(
        PrototypeTableSelector<EntityPrototype>? table,
        System.Random? rand = null,
        EntityTableContext? ctx = null)
    {
        var spawns = GetSpawns(table, rand, ctx).ToList();
        DebugTools.Assert(spawns.Count == 1, "Must have exactly 1 spawn");
        return spawns.First();
    }

    public IEnumerable<EntProtoId> GetSpawns(EntityTablePrototype entTableProto, System.Random? rand = null, EntityTableContext? ctx = null)
    {
        // convenient
        return GetSpawns(entTableProto.Table, rand, ctx);
    }

    public IEnumerable<EntProtoId> GetSpawns(PrototypeTableSelector<EntityPrototype>? table, System.Random? rand = null, EntityTableContext? ctx = null)
    {
        if (table == null)
            return new List<EntProtoId>();

        rand ??= _random.GetRandom();
        ctx ??= new EntityTableContext();
        return table.GetSpawns(rand, EntityManager, _prototypeManager, ctx).Select(p => new EntProtoId(p));
    }
}

/// <summary>
/// Context used by selectors and conditions to evaluate in generic gamestate information.
/// </summary>
public sealed class EntityTableContext
{
    private readonly Dictionary<string, object> _data = new();

    public EntityTableContext()
    {

    }

    public EntityTableContext(Dictionary<string, object> data)
    {
        _data = data;
    }

    /// <summary>
    /// Retrieves an arbitrary piece of data from the context based on a provided key.
    /// </summary>
    /// <param name="key">A string key that corresponds to the value we are searching for. </param>
    /// <param name="value">The value we are trying to extract from the context object</param>
    /// <typeparam name="T">The type of <see cref="value"/> that we are trying to retrieve</typeparam>
    /// <returns>If <see cref="key"/> has a corresponding value of type <see cref="T"/></returns>
    [PublicAPI]
    public bool TryGetData<T>([ForbidLiteral] string key, [NotNullWhen(true)] out T? value)
    {
        value = default;
        if (!_data.TryGetValue(key, out var valueData) || valueData is not T castValueData)
            return false;

        value = castValueData;
        return true;
    }
}
