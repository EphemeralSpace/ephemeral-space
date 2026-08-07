namespace Content.Server._ES.StationVariation.Components;

/// <summary>
///     Rotates the camera of any player that joins the game by the specified angle when this game rule is active.
/// </summary>
[RegisterComponent]
public sealed partial class ESRotatedCameraGameRuleComponent : Component
{
    [DataField]
    public Angle Angle = Angle.FromDegrees(180);
}
