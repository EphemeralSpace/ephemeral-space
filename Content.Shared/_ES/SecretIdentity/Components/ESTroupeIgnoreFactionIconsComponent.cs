using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
///     Any mind with this component will ignore faction icons for the given troupes, even if a member of that troupe.
/// </summary>
/// <see cref="ESTroupeNoSharedObjectivesComponent"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESTroupeIgnoreFactionIconsComponent : Component
{
    /// <summary>
    ///     The troupes for which this mind ignores faction icons.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<ESTroupePrototype>> Troupes = new();
}
