namespace Content.Shared._ES.KillTracking.Components;

/// <summary>
/// Holds info about the entities with <see cref="ESKillTrackerComponent"/> killed by this entity.
/// </summary>
[RegisterComponent]
[Access(typeof(ESKillTrackingSystem))]
public sealed partial class ESKillerTrackerComponent : Component
{
    /// <summary>
    /// Number of players this entity has killed.
    /// </summary>
    [DataField]
    public int KilledPlayerCount;
}
