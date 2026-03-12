namespace Content.Server._ES.Armory.Components;

/// <summary>
///     Marks an entity which has pressed an <see cref="Components.ESArmoryGovernanceInterfaceComponent"/> already and
///     cannot do so again
/// </summary>
/// <see cref="ESArmorySystem"/>
[RegisterComponent]
public sealed partial class ESArmoryPressedButtonAlreadyComponent : Component;
