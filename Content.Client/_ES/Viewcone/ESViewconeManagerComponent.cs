namespace Content.Client._ES.Viewcone;

[RegisterComponent]
public sealed partial class ESViewconeManagerComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ConeAngle;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ConeFeather;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ConeIgnoreRadius;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float ConeIgnoreFeather;
}
