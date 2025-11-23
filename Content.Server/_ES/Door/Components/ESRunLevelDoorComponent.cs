using Content.Server.GameTicking;
using Content.Shared.Doors.Components;

namespace Content.Server._ES.Door.Components;

/// <summary>
/// <see cref="DoorComponent"/> that opens/closes when the run level changes.
/// </summary>
[RegisterComponent]
[Access(typeof(ESRunLevelDoorSystem))]
public sealed partial class ESRunLevelDoorComponent : Component
{
    /// <summary>
    /// The run level that will cause this door to be open.
    /// All other run levels will cause it to close.
    /// </summary>
    [DataField]
    public GameRunLevel OpenRunLevel = GameRunLevel.PreRoundLobby;

    [DataField]
    public bool Invert = true;
}
