using Content.Shared._ES.Tips;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.SecretIdentity;

/// <summary>
/// Denotes a set of objectives, name, desc.
/// Essentially a mini antag thing
/// </summary>
[Prototype("esSecretIdentity")]
public sealed partial class ESSecretIdentityPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESSecretIdentityPrototype>))]
    public string[]? Parents { get; private set; }

    [NeverPushInheritance]
    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Arbitray number used to order which secret identities are assigned before other ones
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

    /// <summary>
    /// Set of tips that apply to this secret identity specifically.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESTipPrototype>> Tips = new();

    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public ComponentRegistry MindComponents = new();

    /// <summary>
    /// Gear applied to player when they receive this secret identity.
    /// </summary>
    [DataField]
    public ProtoId<StartingGearPrototype>? Gear;

    /// <summary>
    /// Actions provided to the player when they receive this secret identity.
    /// Removed when the secret identity is removed.
    /// </summary>
    [DataField]
    public EntityTableSelector Actions = new NoneSelector();

    /// <summary>
    /// Objectives to assign
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = new NoneSelector();

    /// <summary>
    /// Players with any of these jobs will be ineligible for receiving this secret identity
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> ProhibitedJobs = new();
}
