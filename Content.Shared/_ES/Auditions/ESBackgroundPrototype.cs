using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// This is a prototype for marking backgrounds
/// </summary>
[Prototype("esBackground")]
public sealed partial class ESBackgroundPrototype : IPrototype
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
    /// Description of this relationship.
    /// </summary>
    [DataField, ViewVariables]
    public LocId Description;
}
