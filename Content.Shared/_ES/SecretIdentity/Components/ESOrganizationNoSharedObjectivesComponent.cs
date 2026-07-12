using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
///     Any mind with this component will not receive organization objectives for the specified organizations, even if they're part of that organization.
/// </summary>
/// <see cref="ESOrganizationIgnoreFactionIconsComponent"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESOrganizationNoSharedObjectivesComponent : Component
{
    /// <summary>
    ///     The organizations for which this mind cannot receive shared objectives.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<ESOrganizationPrototype>> Organizations = new();
}
