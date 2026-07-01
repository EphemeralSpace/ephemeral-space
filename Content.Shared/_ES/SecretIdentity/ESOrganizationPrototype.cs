using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Tips;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.SecretIdentity;

[Prototype("esOrganization")]
public sealed partial class ESOrganizationPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESOrganizationPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }

    /// <summary>
    /// Name of the organization, in plain text.
    /// </summary>
    [DataField(required: true)]
    public LocId Name;

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Set of tips that apply to this organization specifically.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESTipPrototype>> Tips = new();

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
    /// The objectives that this organization gives to its members
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = new NoneSelector();

    [DataField(required: true)]
    public EntProtoId<ESOrganizationRuleComponent> GameRule;

    /// <summary>
    /// String used to refer to the secret identities of this organization on the news report for the masquerade.
    /// </summary>
    [DataField]
    public LocId? DisguisedSecretIdentityName;
}
