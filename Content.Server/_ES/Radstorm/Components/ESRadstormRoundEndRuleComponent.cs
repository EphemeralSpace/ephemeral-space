using Content.Shared.Damage;
using Robust.Shared.Audio;

namespace Content.Server._ES.Radstorm.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESRadstormRoundEndRuleSystem))]
public sealed partial class ESRadstormRoundEndRuleComponent : Component
{
    [DataField(required: true)]
    public List<ESRadstormPhaseConfig> RadstormPhases = new();

    [DataField(required: true)]
    public DamageSpecifier RadstormDamagePerSecond = new();

    /// <summary>
    ///     Average time that the radstorm can start at. Used when randomly picking <see cref="RadstormTimeRemaining"/>.
    /// </summary>
    [DataField]
    public TimeSpan RadstormStartTimeAvg = TimeSpan.FromMinutes(72f);

    /// <summary>
    ///     Standard deviation for time that the radstorm can start at. Used when randomly picking <see cref="RadstormTimeRemaining"/>.
    /// </summary>
    [DataField]
    public TimeSpan RadstormStartTimeStdDev = TimeSpan.FromMinutes(2f);

    /// <summary>
    ///     Time at which <see cref="RadstormTimeRemaining"/> will update and phases will be updated
    /// </summary>
    [DataField, AutoPausedField]
    public TimeSpan NextUpdateTime;

    /// <summary>
    ///     Time inbetween updates.
    /// </summary>
    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(1);

    /// <summary>
    ///     Picked randomly when the rule is added. Amount of time left until the radstorm should start (i.e. when people should start dying).
    ///     Note that this does not decrease linearly, as it varies based on the radstorm modifiers.
    /// </summary>
    /// <remarks>
    ///     You are not really intended to write to this from YAML, but if you do, it won't be overridden.
    /// </remarks>
    [DataField]
    public TimeSpan RadstormTimeRemaining = TimeSpan.Zero;

    /// <summary>
    ///     The total unadjusted time it takes for the radstorm to arrive.
    /// </summary>
    [DataField]
    public TimeSpan RadstormDuration = TimeSpan.Zero;

    /// <summary>
    ///     Time that has passed for the radstorm so far.
    /// </summary>
    [ViewVariables]
    public TimeSpan ElapsedRadstormTime => RadstormDuration - RadstormTimeRemaining;

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
public sealed partial class ESRadstormPhaseConfig
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
    public bool RemoveGridRoof;

    [DataField]
    public bool SpaceDangerous;
}
