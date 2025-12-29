using Content.Shared.EntityTable.EntitySelectors;

namespace Content.Server._ES.SpawnRegion.Components;

/// <summary>
/// This is a game rule that just randomly spawns entities across the station.
/// Dead simple and not very useful besides placeholder-y stuff
/// </summary>
[RegisterComponent]
[Access(typeof(ESSpawnRandomRule))]
public sealed partial class ESSpawnRandomRuleComponent : Component
{
    /// <summary>
    /// Entities that will be spawned
    /// </summary>
    [DataField]
    public EntityTableSelector Table = new NoneSelector();
}
