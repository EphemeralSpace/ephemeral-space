using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is a prototype for marking relationships
/// </summary>
[Prototype("esRelationship")]
public sealed partial class ESRelationshipPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Name of this relationship.
    /// </summary>
    [DataField, ViewVariables]
    public LocId Name;

    /// <summary>
    /// Color of this relationship.
    /// </summary>
    [DataField, ViewVariables]
    public Color Color;

    /// <summary>
    /// Whether or not this relationship must be mutual. Family members, ex-lovers, etc, all fall under this.
    /// </summary>
    [DataField, ViewVariables]
    public bool ForceMutual;
}
