using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server._ES.Radstorm;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESRadstormRoundEndRuleComponent : Component
{
    [DataField(required: true)]
    public List<ESRadstormPhaseConfig> RadstormPhases = new();

    [DataField(required: true)]
    public DamageSpecifier RadstormDamagePerSecond = new();

    /// <summary>
    ///     Average time that the radstorm can start at. Used when randomly picking <see cref="RadstormStartTime"/>.
    /// </summary>
    [DataField]
    public TimeSpan RadstormStartTimeAvg = TimeSpan.FromMinutes(60f);

    /// <summary>
    ///     Standard deviation for time that the radstorm can start at. Used when randomly picking <see cref="RadstormStartTime"/>.
    /// </summary>
    [DataField]
    public TimeSpan RadstormStartTimeStdDev = TimeSpan.FromMinutes(2f);

    /// <summary>
    ///     Picked randomly when the rule is added. Time into the round that the radstorm should start (i.e. when people should start dying),
    ///     and time relative to which the phases should be announced.
    /// </summary>
    /// <remarks>
    ///     You are not really intended to write to this from YAML, but if you do, it won't be overridden.
    /// </remarks>
    [DataField, AutoPausedField]
    public TimeSpan RadstormStartTime = TimeSpan.Zero;

    /// <summary>
    ///     Time that the next radstorm damage tick should occur. Written to when the radstorm starts.
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan RadstormNextDamageTickTime = TimeSpan.Zero;

    /// <summary>
    ///     If a phase ran which marked space as dangerous, this will be true, and entities in space
    ///     even if it hasn't fully started yet.
    /// </summary>
    public bool SpaceDangerous = false;
}

// no this cant be a fucking record because apparently you cant have datarecords that also have properties.
[DataDefinition]
public partial class ESRadstormPhaseConfig
{
    public bool Completed = false;

    [DataField]
    public TimeSpan? TimeBeforeEnd;

    /// <summary>
    ///     Optional, allows you to have a phase relative to roundstart rather than from the end.
    /// </summary>
    [DataField]
    public TimeSpan? TimeAfterStart;

    [DataField]
    public float AnnouncementDistortion;

    [DataField]
    public LocId? AnnouncementText;

    [DataField]
    public SoundSpecifier? AnnouncementSound;

    [DataField]
    public Color? MapLight;

    [DataField]
    public bool SpaceDangerous;
}
