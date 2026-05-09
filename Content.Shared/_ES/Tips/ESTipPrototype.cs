using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Tips;

/// <summary>
///     Represents a single tip
/// </summary>
[Prototype("esTip")]
public sealed partial class ESTipPrototype : IPrototype
{
    [IdDataField]
    public string ID { get; set; } = default!;

    /// <summary>
    ///     Not localized. Use <see cref="ESTipsManager"/> to get a properly localized version.
    ///     If you want to override the localization text for a tip, set a loc ID like `es-tip-{tip prototype id}`.
    /// </summary>
    [DataField("text", required: true), Access(typeof(ESTipsManager))]
    public string UnlocalizedText = default!;

    /// <summary>
    ///     If true, this tip will never be shown randomly.
    ///     However, it can still be referred to by guidebook embeds, etc.
    /// </summary>
    [DataField]
    public bool Hidden = false;
}
