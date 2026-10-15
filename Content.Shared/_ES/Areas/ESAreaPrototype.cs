using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Areas;

[Prototype("esArea")]
public sealed partial class ESAreaPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField]
    public Color DebugColor = Color.White;
}
