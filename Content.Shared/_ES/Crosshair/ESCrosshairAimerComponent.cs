namespace Content.Shared._ES.Crosshair;

/// <summary>
///     An entity which can aim with a crosshair.
/// </summary>
[RegisterComponent]
public sealed partial class ESCrosshairAimerComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? CrosshairEntity;
}
