namespace Content.Server._ES.WarpDrive.Components;

/// <summary>
///     Marks an item considered 'invasive' in the singularity world which prevents the warp drive from charging
///     Removed once picked up by anyone
/// </summary>
[RegisterComponent]
public sealed partial class ESSingularityWorldInterruptionComponent : Component;
