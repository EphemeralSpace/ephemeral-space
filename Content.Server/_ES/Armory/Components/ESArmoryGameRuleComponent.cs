using Robust.Shared.Audio;

namespace Content.Server._ES.Armory.Components;

/// <summary>
///     Handles global state for armory stuff (cooldown etc) instead of trying to sync it between all the buttons
/// </summary>
// todo this really shouldnt be a gamerule thing id prefer if it was just a singleton ent that gets spawned if theres any button
// wait for moonys engine stuff i guess
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESArmoryGameRuleComponent : Component
{
    /// <summary>
    ///     Null if the armory is not on cooldown.
    ///     Otherwise, pressing any buttons will be disallowed if before this time (set after a failed armory opening sequence)
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan? ArmoryCooldownLiftsAt = TimeSpan.Zero;

    /// <summary>
    ///     Amount of time after a failed armory open before the button will be pressable again
    /// </summary>
    [DataField]
    public TimeSpan ArmoryCooldownTime = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Time after a button was pressed in which the other buttons are valid presses for continuing the opening sequence
    ///     This only matters whenever the first button is pressed (i.e. you cant get more leeway by pressing a second button)
    /// </summary>
    [DataField]
    public TimeSpan ButtonPressAllTimeframe = TimeSpan.FromSeconds(2.5);

    /// <summary>
    ///     Delay before the armory doors actually open
    /// </summary>
    [DataField]
    public TimeSpan ArmoryOpenDelay = TimeSpan.FromSeconds(20);

    /// <summary>
    ///     Set if the armory is open
    /// </summary>
    [DataField]
    public bool Opened = false;

    // various

    /// <summary>
    ///     How long people in range will be electrocuted for after a failed
    /// </summary>
    // Who up failwrithing
    [DataField, AutoNetworkedField]
    public TimeSpan FailWritheDuration = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier ArmoryOpeningAnnouncementSound =
        new SoundPathSpecifier("/Audio/_ES/Announcements/attention_high.ogg");

    [DataField]
    public SoundSpecifier ArmoryOpenedAnnouncementSound =
        new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg");

    [DataField]
    public SoundSpecifier ArmoryFailedAnnouncementSound =
        new SoundPathSpecifier("/Audio/_ES/Announcements/attention_medium.ogg");
}
