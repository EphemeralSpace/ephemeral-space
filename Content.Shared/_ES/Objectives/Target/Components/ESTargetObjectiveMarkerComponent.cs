namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// Marker component with <see cref="ESTargetObjectiveComponent"/> used to update relevant objectives on lifestage changes.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetObjectiveSystem), Other = AccessPermissions.None)]
public sealed partial class ESTargetObjectiveMarkerComponent : Component
{
    [DataField]
    public HashSet<EntityUid> Objectives = new();
}
