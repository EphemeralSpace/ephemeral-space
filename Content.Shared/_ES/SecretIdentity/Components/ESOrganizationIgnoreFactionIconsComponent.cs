using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
///     Any mind with this component will ignore faction icons for the given organizations, even if a member of that organization.
/// </summary>
/// <see cref="ESOrganizationNoSharedObjectivesComponent"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESOrganizationIgnoreFactionIconsComponent : Component
{
    /// <summary>
    ///     The organizations for which this mind ignores faction icons.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<ESOrganizationPrototype>> Organizations = new();
}
