using Robust.Shared.GameStates;

namespace Content.Shared._ES.DoorJammer;

/// <summary>
///     Bolts a door while embedded in it.
///     Once removed, the door is unbolted, assuming that the door wasn't already bolted.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESDoorJammerComponent : Component
{
    /// <summary>
    ///     Non-null while we're embedded in a door.
    ///     If true, the door was already bolted when we were embedded, and so we shouldn't unbolt the door.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool? WasAlreadyBolted;
}
