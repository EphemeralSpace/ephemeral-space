using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// Groups together skin colors alongside textual descriptions.
/// </summary>
[Prototype("esSkinColor")]
public sealed partial class ESSkinColorPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// User-facing name of this group of skin colors
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// Probability weight
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// List of possible colors
    /// </summary>
    [DataField]
    public List<Color> Colors = new();
}
