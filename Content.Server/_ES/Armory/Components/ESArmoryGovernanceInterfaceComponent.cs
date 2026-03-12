namespace Content.Server._ES.Armory.Components;

/// <summary>
///     Marks a button which, when interacted with, starts the armory opening sequence, where all other buttons
///     must be pressed within a given timeframe
/// </summary>
/// <see cref="ESArmorySystem"/>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESArmoryGovernanceInterfaceComponent : Component
{
    /// <summary>
    ///     Null if this button hasnt been pressed yet / isnt valid to be pressed
    ///     Otherwise, the time this button was pressed at
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan? ButtonPressedAt = TimeSpan.Zero;
}
