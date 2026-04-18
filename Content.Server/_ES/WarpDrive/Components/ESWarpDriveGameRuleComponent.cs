using Robust.Shared.Utility;

namespace Content.Server._ES.WarpDrive.Components;

/// <summary>
///     Controls the warp drive behavior (crew objective, spawning the inner map properly, handling the portals, charging, etc)
/// </summary>
[RegisterComponent]
public sealed partial class ESWarpDriveGameRuleComponent : Component
{
    /// <summary>
    ///     Base charge time if there were literally 0 interruptions (which there will be)
    ///     ~Essentially a lower bound on crew win time
    /// </summary>
    public TimeSpan BaseChargeTime = TimeSpan.FromMinutes(40);

    /// <summary>
    ///     How long a warp drive interruption event can last before it (violently) ends on its own
    /// </summary>
    public TimeSpan InterruptionMaxTime = TimeSpan.FromMinutes(6);

    /// <summary>
    ///     Like nuke defense but for crew. After the drive is fully charged,
    /// </summary>
    public TimeSpan FinalPhaseTime = TimeSpan.FromMinutes(3);

    /// <summary>
    ///     After something / someone enters the singularity world, how long before they're teleported out
    /// </summary>
    public TimeSpan SingularityWorldTeleportOutTime = TimeSpan.FromSeconds(30);

    /// <summary>
    ///     where it all goes
    /// </summary>
    public ResPath SingularityWorldMap = new("/Maps/_ES/singularity_world.yml");
}
