using System.Numerics;
using Robust.Shared.GameStates;
using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Crosshair;

/// <summary>
///     Used to mark a crosshair entity which is tied to some specific player.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCrosshairEntityComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? User;

    /// <summary>
    ///     client-only, we offset the sprite in frameupdate to lerp it and store this as the 'real' pos while having the predicted stuff actually affect transform pos
    /// </summary>
    public Vector2? LerpPos;
}

[Serializable, NetSerializable]
public enum ESCrosshairVisuals : byte
{
    Name
}
