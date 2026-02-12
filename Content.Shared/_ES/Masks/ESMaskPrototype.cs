using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
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
    public string ID { get; private set; }  = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESMaskPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Arbitray number used to order which masks are assigned before other ones
    /// </summary>
    [DataField]
    public int AssignmentOrder = 1;

    /// <summary>
    /// Selection weight
    /// </summary>
    [DataField]
    public float Weight = 1;

    /// <summary>
    /// UI Name
    /// </summary>
    [DataField]
    public LocId Name;

    /// <summary>
    /// UI Color
    /// </summary>
    [DataField(required: true)]
    public Color Color = Color.White;

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
    /// Items spawned in the player's bag when they receive this mask.
    /// </summary>
    [DataField]
    public EntityTableSelector Gear = new NoneSelector();

    /// <summary>
    /// Objectives to assign
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = new NoneSelector();

    /// <summary>
    /// Players with any of these jobs will be ineligible for receiving this mask
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> ProhibitedJobs = new();
}
