using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions.Traits;

[Prototype("esTraitCategory")]
public sealed partial class ESTraitCategoryPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    [DataField(required: true)]
    public float Prob;
}
