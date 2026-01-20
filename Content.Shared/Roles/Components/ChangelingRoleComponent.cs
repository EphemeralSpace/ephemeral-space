using Content.Shared.Actions.Components;
using Robust.Shared.GameStates;

namespace Content.Shared.Roles.Components;

/// <summary>
/// Added to mind role entities to tag that they are a changeling.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ChangelingRoleComponent : BaseMindRoleComponent
// ES Start
{
    [DataField]
    public string? StatisAction = "ActionExistStatis";

    [DataField]
    public EntityUid? StatisActionEntity;
}

