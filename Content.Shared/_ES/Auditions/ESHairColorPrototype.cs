using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.Auditions;

/// <summary>
/// Groups a variety of colors together under a shared "color" of hair.
/// So all blonde hair is in here, all gray hair, etc.
/// </summary>
[Prototype("esHairColor")]
public sealed partial class ESHairColorPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESHairColorPrototype>))]
    public string[]? Parents { get; private set; }

    /// <inheritdoc />
    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// User-facing name of this hair color
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// Weight compared to other hair colors
    /// </summary>
    [DataField]
    public float Weight = 1f;

    /// <summary>
    /// Minimum age someone must be to be assigned this hair color.
    /// </summary>
    [DataField]
    public int MinAge;

    /// <summary>
    /// Maximum age someone must be under to be assigned this hair color.
    /// </summary>
    [DataField]
    public int MaxAge = 120;

    /// <summary>
    /// List of possible colors for general type of hair.
    /// </summary>
    [DataField]
    public List<Color> Colors = new();
}
