using Content.Shared.Doors.Components;

namespace Content.Server._ES.Armory.Components;

/// <summary>
///     Marks a <see cref="DoorComponent"/> which will be bolted after any <see cref="ESArmoryGovernanceInterfaceComponent"/>s are interacted with,
///     and which will only be unbolted on a failed or succeeded armory sequence
/// </summary>
[RegisterComponent]
public sealed partial class ESArmoryControlRoomDoorComponent : Component;
