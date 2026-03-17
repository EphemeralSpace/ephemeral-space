using Content.Shared.Doors.Components;

namespace Content.Server._ES.Armory.Components;

/// <summary>
///     Marks a <see cref="DoorComponent"/> which should bolt open once the armory sequence is finished
/// </summary>
/// <see cref="ESArmorySystem"/>
[RegisterComponent]
public sealed partial class ESArmoryDoorComponent : Component;
