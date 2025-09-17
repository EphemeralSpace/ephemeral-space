using Robust.Shared.GameStates;

namespace Content.Shared._ES.Viewcone;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ViewconeComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ConeAngle = 270f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ConeFeather = 10f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ConeIgnoreRadius = 0.85f;

    [DataField, ViewVariables(VVAccess.ReadWrite), AutoNetworkedField]
    public float ConeIgnoreFeather = 0.25f;
}
