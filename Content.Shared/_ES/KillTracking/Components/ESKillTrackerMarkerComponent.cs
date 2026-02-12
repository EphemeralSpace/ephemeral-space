namespace Content.Shared._ES.KillTracking.Components;

[RegisterComponent]
[Access(typeof(ESKillTrackingSystem))]
public sealed partial class ESKillTrackerMarkerComponent : Component
{
    /// <summary>
    /// All the entities which have been hurt by this entity.
    /// </summary>
    [DataField]
    public HashSet<EntityUid> HurtEntities = [];
}
