using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Handles assigning secret identities to players when they join into the round.
/// </summary>
/// <remarks>
/// Logic only present on server.
/// </remarks>
[RegisterComponent]
[Access(typeof(ESSharedSecretIdentitySystem))]
public sealed partial class ESOrganizationRuleComponent : Component
{
    /// <summary>
    /// Priority for the assignment of players.
    /// Rules with equal priority will be assigned simultaneously.
    /// </summary>
    [DataField]
    public int Priority = 1;

    /// <summary>
    /// Organization that is associated with this rule
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESOrganizationPrototype> Organization;

    /// <summary>
    /// Minds that are a part of this organization.
    /// </summary>
    [DataField]
    public List<EntityUid> OrganizationMemberMinds = new();
}
