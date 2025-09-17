using Robust.Shared.GameStates;

namespace Content.Shared._ES.Viewcone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESViewconeOccludedComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseAlpha = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public bool OccludeIfAnchored = false;
}
