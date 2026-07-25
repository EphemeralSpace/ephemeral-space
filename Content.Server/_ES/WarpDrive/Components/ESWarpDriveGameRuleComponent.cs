using Content.Shared.EntityTable;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Utility;

namespace Content.Server._ES.WarpDrive.Components;

/// <summary>
///     Controls the warp drive behavior (crew objective, spawning the inner map properly, handling the portals, charging, etc)
/// </summary>
[RegisterComponent]
public sealed partial class ESWarpDriveGameRuleComponent : Component
{
    [DataField(required: true)]
    public List<ESWarpDriveAnnouncementConfig> Announcements = new();

    /// <summary>
    ///     Main interaction with the warp drive.
    ///     Interruptions can be random, or manually caused by throwing items in.
    /// </summary>
    [DataField]
    public bool Interrupted;

    /// <summary>
    ///     The time we were last interrupted at.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? LastInterruptionTime;

    /// <summary>
    ///     At start and after each interruption is quelled, picks a random time for a new interruption.
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? NextInterruptionTime;

    /// <summary>
    ///     Accumulated time spent interrupted, to subtract.
    /// </summary>
    [DataField]
    public TimeSpan AccumulatedInterruptionTime = TimeSpan.Zero;

    [DataField]
    public bool InFinalPhase;

    /// <summary>
    ///     IF in final phase, the time we entered it at/ whatever
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? FinalPhaseAt;

    /// <summary>
    ///     Number of terminals currently overridden, if drive is charged.
    /// </summary>
    [DataField]
    public int TerminalsOverridden = 0;

    /// <summary>
    ///     If any terminals have been overridden, the time the first one was overridden at
    /// </summary>
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? FirstTerminalOverriddenAt;

    /// <summary>
    ///     Amount of time after first terminal overridden that crew has to override the others.
    /// </summary>
    [DataField]
    public TimeSpan TerminalOverrideTime = TimeSpan.FromMinutes(2);

    /// <summary>
    ///     Set whenever all 3 terminals have been overridden successfully.
    /// </summary>
    [DataField]
    public bool AllTerminalsOverridden = false;

    /// <summary>
    ///     Used to calculate if an interruption should occur from manual sabotage.
    /// </summary>
    [DataField]
    public int ItemsTeleportedSinceLastInterruption;

    /// <summary>
    ///     Base charge time if there were literally 0 interruptions (which there will be)
    ///     ~Essentially a lower bound on crew win time
    /// </summary>
    [DataField]
    public TimeSpan BaseChargeTime = TimeSpan.FromMinutes(48);

    /// <summary>
    ///     Like nuke defense but for crew. After the drive is fully charged, this timer starts and the win only
    ///     occurs after
    /// </summary>
    [DataField]
    public TimeSpan FinalPhaseTime = TimeSpan.FromMinutes(1);

    /// <summary>
    ///     Min amount of time between random interruptions.
    /// </summary>
    [DataField]
    public TimeSpan MinRandomInterruptionTime = TimeSpan.FromMinutes(5);

    /// <summary>
    ///     Max amount of time between random interruptions.
    /// </summary>
    [DataField]
    public TimeSpan MaxRandomInterruptionTime = TimeSpan.FromMinutes(10);

    /// <summary>
    ///     How long a warp drive interruption event can last before it (violently) ends on its own
    /// </summary>
    //[DataField]
    //public TimeSpan InterruptionForceEndTime = TimeSpan.FromMinutes(5);

    [DataField]
    public int MinInterruptionTrashSpawns = 3;

    [DataField]
    public int MaxInterruptionTrashSpawns = 7;

    /// <summary>
    ///     How many entities to be thrown into the warp drive to cause an interruption.
    /// </summary>
    [DataField]
    public int ManualInterruptionItems = 5;

    /// <summary>
    ///     The percentage the warp drive was charged at the last time a screen packet was sent out.
    ///     Saved and checked to ensure we don't send too many screen updates, and instead only do it if the
    ///     percentage changes by enough (5% atm)
    /// </summary>
    /// <remarks>
    ///     Int to avoid getting float precision fucked
    /// </remarks>
    [DataField]
    public int LastScreenUpdatedChargePercentage;

    /// <summary>
    ///     where it all goes
    /// </summary>
    [DataField]
    public ResPath SingularityWorldMap = new("/Maps/_ES/singularity_world.yml");

    [DataField]
    public ProtoId<EntityTablePrototype> InterruptionTrashTable = "ESMaintLootExposed";

    /// <summary>
    ///     last person that picked up clogged item
    /// </summary>
    [DataField]
    public EntityUid? LastClearer;

    public bool CinematicPlayed = false;
}

[DataDefinition]
public sealed partial class ESWarpDriveAnnouncementConfig
{
    public bool Completed = false;

    /// <summary>
    ///     What % of charge should this play at? (mostly correlated with time)
    /// </summary>
    [DataField]
    public float? AfterChargePercentage;

    /// <summary>
    ///     If non-null, this announcement will play only in final phase and after a certain amount of
    ///     the final phase has completed.
    /// </summary>
    [DataField]
    public float? AfterFinalPhasePercentage;

    [DataField(required: true)]
    public LocId Text;

    [DataField(required: true)]
    public SoundSpecifier Sound;

    [DataField]
    public bool UpdateTerminals = false;
}
