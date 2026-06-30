using Robust.Shared.GameStates;
using Robust.Shared.Map;

namespace Content.Shared._ES.Crosshair;

/// <summary>
///     An entity which has the capability of spawning a
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCrosshairAimerComponent : Component
{
    /// <summary>
    ///     Null if the user is not currently aiming.
    /// </summary>
    [DataField, AutoNetworkedField]
    public EntityUid? CrosshairEntity;
}
