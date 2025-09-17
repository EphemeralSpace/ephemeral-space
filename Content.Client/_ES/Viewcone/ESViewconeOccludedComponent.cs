namespace Content.Client._ES.Viewcone;

[RegisterComponent]
public sealed partial class ESViewconeOccludedComponent : Component
{
    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public float BaseAlpha = 1.0f;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public bool OccludeIfAnchored = false;
}
