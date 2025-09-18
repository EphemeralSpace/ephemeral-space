namespace Content.Client._ES.Viewcone;

[RegisterComponent]
public sealed partial class ESViewconeComponent : Component
{
    [DataField]
    public float ConeAngle = 270f;

    [DataField]
    public float ConeFeather = 10f;

    [DataField]
    public float ConeIgnoreRadius = 0.85f;

    [DataField]
    public float ConeIgnoreFeather = 0.25f;
}
