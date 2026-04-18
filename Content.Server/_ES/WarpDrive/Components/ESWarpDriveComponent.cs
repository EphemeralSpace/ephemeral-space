namespace Content.Server._ES.WarpDrive.Components;

/// <summary>
///     The actual warp drive entity, which teleports stuff on collision
///     Most actual data is just stored on the rule entity
/// </summary>
[RegisterComponent]
public sealed partial class ESWarpDriveComponent : Component
{
    /// <summary>
    ///     After something / someone enters the singularity world, how long before they're teleported out
    /// </summary>
    public TimeSpan SingularityWorldTeleportOutTime = TimeSpan.FromSeconds(20);
}
