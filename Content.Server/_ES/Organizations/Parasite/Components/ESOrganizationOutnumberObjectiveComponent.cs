using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Organizations.Parasite.Components;

/// <summary>
/// An objective that is measured by the number of living organization over the number of other organization members.
/// </summary>
[RegisterComponent]
[Access(typeof(ESOrganizationOutnumberObjectiveSystem))]
public sealed partial class ESOrganizationOutnumberObjectiveComponent : Component
{
    [DataField]
    public ProtoId<ESOrganizationPrototype> Organization;

    [DataField]
    public float TargetPercentage = 0.5f;
}
