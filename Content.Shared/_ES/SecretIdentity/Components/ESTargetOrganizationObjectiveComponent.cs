using Content.Shared._ES.Objectives.Target.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Used with <see cref="ESTargetObjectiveComponent"/> to filter based on a player's organization
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetOrganizationObjectiveSystem))]
public sealed partial class ESTargetOrganizationObjectiveComponent : Component
{
    /// <summary>
    /// The organization that must be present on the player
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESOrganizationPrototype> Organization;

    /// <summary>
    /// If true, will select if a given player does NOT have <see cref="Organization"/> as their organization
    /// </summary>
    [DataField]
    public bool Invert;

    /// <summary>
    /// If a given player has any secret identity in this set, they will NOT be invalidated, even if they otherwise would be.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESSecretIdentityPrototype>> OverrideSecretIdentities = new();
}
