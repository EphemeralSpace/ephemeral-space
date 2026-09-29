using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Auditions.Traits;

[Prototype("esTrait")]
public sealed partial class ESTraitPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; } = default!;

    /// <summary>
    /// Category for exclusive traits
    /// </summary>
    [DataField]
    public ProtoId<ESTraitCategoryPrototype>? Category;

    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Dependent on <see cref="Category"/>.
    /// If a category is specified, then this will be the weight of the item within the category.
    /// If no category is specified, then this is the chance that the trait will be applied.
    /// </summary>
    [DataField(required: true)]
    public float Prob = 1.0f;

    [DataField]
    public List<ESTraitEvent> Events = [];
}

[ImplicitDataDefinitionForInheritors]
public abstract partial class ESTraitEvent : EntityEventArgs;
