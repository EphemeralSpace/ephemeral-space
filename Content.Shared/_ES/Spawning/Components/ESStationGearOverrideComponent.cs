using Content.Shared.Roles;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Spawning.Components;

/// <summary>
/// Used to add equipment overrides for jobs on a station
/// </summary>
[RegisterComponent]
public sealed partial class ESStationGearOverrideComponent : Component
{
    /// <summary>
    /// Maps a job to a starting gear that overrides its base equipment.
    /// </summary>
    [DataField]
    public Dictionary<ProtoId<JobPrototype>, ProtoId<StartingGearPrototype>> Overrides = new();
}
