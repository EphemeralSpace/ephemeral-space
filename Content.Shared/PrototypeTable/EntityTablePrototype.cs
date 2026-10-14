using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared.PrototypeTable;

/// <summary>
/// Prototype version of a <see cref="PrototypeTableSelector{T}"/> containing entity prototypes, for reuse in <see cref="EntityNestedSelector"/>
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
