using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Marks organizations that the entity is hostile towards, namely for targeting specfic organizations in mob behavior
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedSecretIdentitySystem))]
public sealed partial class ESHostileTowardsOrganizationComponent : Component
{
    /// <summary>
    /// If specified, all members not of this organization will be hostile.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESOrganizationPrototype>>? NonHostileOrganizations;

    [DataField]
    public HashSet<ProtoId<ESOrganizationPrototype>>? HostileOrganizations;
}
