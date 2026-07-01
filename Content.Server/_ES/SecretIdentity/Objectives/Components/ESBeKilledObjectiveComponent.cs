using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Objectives.Components;

/// <summary>
///     Sets counter to 1 if the owner was killed by someone else--not just any death.
/// </summary>
[RegisterComponent]
public sealed partial class ESBeKilledObjectiveComponent : Component
{
    /// <summary>
    ///     If non-null, the killer must be of this organization in order to count as a completion.
    /// </summary>
    [DataField]
    public ProtoId<ESOrganizationPrototype>? OrganizationRequired = null;
}
