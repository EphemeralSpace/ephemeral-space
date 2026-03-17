using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

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

    /// <summary>
    ///     If non-null, will use this spawn region to choose locations
    /// </summary>
    [DataField]
    public ProtoId<ESSpawnRegionPrototype>? SpawnRegion = null;

    /// <summary>
    ///     Whether to avoid spawning if a player can see the spawn location.
    /// </summary>
    [DataField]
    public bool CheckPlayerLOS = false;

    /// <summary>
    ///     In what radius around players we should avoid spawning an entity
    /// </summary>
    /// <returns></returns>
    [DataField]
    public float MinPlayerDistance = 2.5f;
}
