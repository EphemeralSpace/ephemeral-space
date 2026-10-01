using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Radstorm.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESNavigationConsoleComponent : Component
{
    [DataField]
    public EntProtoId TurbulenceRule = "ESDegradationEventSpaceTurbulence";
}
