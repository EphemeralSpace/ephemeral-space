using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Roles;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.Masks;

[Prototype("esTroupe")]
public sealed partial class ESTroupePrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESTroupePrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Name of the troupe, in plain text.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Color used in UI
    /// </summary>
    [DataField]
    public Color Color = Color.White;

    /// <summary>
    /// Meta-game icon used by stagehands when observing.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<FactionIconPrototype> MetaIcon;

    /// <summary>
    /// Players with any of these jobs will be ineligible for being members of this troupe
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> ProhibitedJobs = new();

    /// <summary>
    /// The objectives that this troupe gives to its members
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = new NoneSelector();
}
