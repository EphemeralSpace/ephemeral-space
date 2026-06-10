using Robust.Shared.Audio;

namespace Content.Server._ES.FirstDeath;

/// <summary>
///     Plays a sound globally when the first player in the round dies (i.e. a kill is reported)
/// </summary>
[RegisterComponent]
public sealed partial class ESFirstDeathAnnouncementGameRuleComponent : Component
{
    /// <summary>
    ///     The sound to play globally.
    /// </summary>
    [DataField]
    public SoundSpecifier Sound = new SoundPathSpecifier("/Audio/_ES/Announcements/first_death.ogg");

    /// <summary>
    ///     Whether this rule has played its sound or not.
    /// </summary>
    [ViewVariables]
    public bool PlayedSound = false;
}
