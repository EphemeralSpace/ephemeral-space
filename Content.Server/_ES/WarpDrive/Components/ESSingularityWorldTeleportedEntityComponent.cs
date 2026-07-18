namespace Content.Server._ES.WarpDrive.Components;

/// <summary>
///     Marks an entity which was teleported into the singularity world, so they can
///     be teleported out automatically
///     Also handles adding this comp to any dropped entities
/// </summary>
[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESSingularityWorldTeleportedEntityComponent : Component
{
    [DataField, AutoPausedField]
    public TimeSpan TeleportOutTime;
}
