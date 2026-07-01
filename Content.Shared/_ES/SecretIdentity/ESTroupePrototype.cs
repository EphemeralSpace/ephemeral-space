using Content.Shared._ES.SecretIdentity.Components;
using Content.Shared._ES.Tips;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.StatusIcon;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Shared._ES.SecretIdentity;

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

    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Set of tips that apply to this troupe specifically.
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
    /// The objectives that this troupe gives to its members
    /// </summary>
    [DataField]
    public EntityTableSelector Objectives = new NoneSelector();

    [DataField(required: true)]
    public EntProtoId<ESTroupeRuleComponent> GameRule;

    /// <summary>
    /// String used to refer to the secret identities of this troupe on the news report for the masquerade.
    /// </summary>
    [DataField]
    public LocId? DisguisedSecretIdentityName;
}
