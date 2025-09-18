using Content.Shared.Roles;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.Masks;

[Prototype("esTroupe")]
public sealed partial class ESTroupePrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESTroupePrototype>))]
    public string[]? Parents { get; }

    [AbstractDataField]
    public bool Abstract { get; }

    /// <summary>
    /// Name of the troupe, in plain text.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    /// <summary>
    /// Players with any of these jobs will be ineligible for being members of this troupe
    /// </summary>
    [DataField]
    public HashSet<ProtoId<JobPrototype>> ProhibitedJobs = new();
}
