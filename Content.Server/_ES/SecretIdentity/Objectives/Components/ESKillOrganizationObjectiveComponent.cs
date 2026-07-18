using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Objectives.Components;

/// <summary>
///     Objective for killing a member of a given organization.
///     The target kill count is set by <see cref="NumberObjectiveComponent"/>.
/// </summary>
/// <seealso cref="ESKillOrganizationObjectiveSystem"/>
[RegisterComponent]
[Access(typeof(ESKillOrganizationObjectiveSystem))]
public sealed partial class ESKillOrganizationObjectiveComponent : Component
{
    /// <summary>
    ///     The organization the victim must be a part of
    /// </summary>
    [DataField]
    public ProtoId<ESOrganizationPrototype> Organization;

    /// <summary>
    ///     If true, kills will count if the victim is NOT part of <see cref="Organization"/>
    /// </summary>
    [DataField]
    public bool Invert;
}
