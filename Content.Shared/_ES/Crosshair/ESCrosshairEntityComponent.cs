using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._ES.Crosshair;

/// <summary>
///     Used to mark a crosshair entity which is tied to some specific player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCrosshairEntityComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    public MapCoordinates Target = MapCoordinates.Nullspace;
}
