using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.Masks;

/// <summary>
/// Denotes a set of objectives, name, desc.
/// Essentially a mini antag thing
/// </summary>
[Prototype("esMask")]
public sealed partial class ESMaskPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESMaskPrototype>))]
    public string[]? Parents { get; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// Selection weight
    /// </summary>
    [DataField]
    public int Weight = 1;

    /// <summary>
    /// UI Name
    /// </summary>
    [DataField]
    public LocId Name;

    [DataField]
    public ProtoId<ESTroupePrototype> Troupe;

    /// <summary>
    /// Description of what this role does.
    /// </summary>
    [DataField]
    public LocId Description;

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public ComponentRegistry MindComponents = new();

    /// <summary>
    /// Objectives to assign
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = new NoneSelector();
}
