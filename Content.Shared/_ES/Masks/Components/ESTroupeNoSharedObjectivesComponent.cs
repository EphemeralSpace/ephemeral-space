using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
///     Any mind with this component will not receive troupe objectives for the specified troupes, even if they're part of that troupe.
/// </summary>
/// <see cref="ESTroupeIgnoreFactionIconsComponent"/>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESTroupeNoSharedObjectivesComponent : Component
{
    /// <summary>
    ///     The troupes for which this mind cannot receive shared objectives.
    /// </summary>
    [DataField(required: true)]
    public HashSet<ProtoId<ESTroupePrototype>> Troupes = new();
}
