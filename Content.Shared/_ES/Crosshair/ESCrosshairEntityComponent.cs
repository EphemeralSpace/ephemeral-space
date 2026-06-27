namespace Content.Shared._ES.Crosshair;

/// <summary>
///     Used to mark a crosshair entity which is tied to some specific player.
/// </summary>
[RegisterComponent]
public sealed partial class ESCrosshairEntityComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? User;
}
