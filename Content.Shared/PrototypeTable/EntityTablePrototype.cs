using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable;

/// <summary>
/// This is a prototype for...
/// </summary>
[Prototype]
public sealed partial class EntityTablePrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public PrototypeTableSelector<EntityPrototype> Table = default!;
}
