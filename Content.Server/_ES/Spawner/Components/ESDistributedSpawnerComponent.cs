using Content.Server.Spawners.Components;
using Content.Shared._ES.Core.Entity;
using Content.Shared.EntityTable.EntitySelectors;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Spawner.Components;

/// <summary>
/// Variant of <see cref="EntityTableSpawnerComponent"/> that spawns it's contents across multiple associated <see cref="ESDistributedSpawnerMarkerComponent"/> entity.
/// </summary>
[RegisterComponent, EntityCategory("Spawner")]
[Access(typeof(ESSpawnerSystem))]
public sealed partial class ESDistributedSpawnerComponent : Component
{
    /// <summary>
    /// ID that is matched with <see cref="ESDistributedSpawnerMarkerComponent.Id"/> to determine where to place spawns.
    /// </summary>
    [DataField(required: true)]
    public ProtoId<TagPrototype> Id;

    /// <summary>
    /// Used to determine which nearby markers are in-range and able to be used
    /// </summary>
    [DataField]
    public float Range = 7f;

    /// <summary>
    /// The strategy used for determining how the entities will be spawned.
    /// </summary>
    [DataField]
    public ESSpawnStrategy Strategy = ESSpawnStrategy.Diagonal;

    /// <summary>
    /// List of tables that are used for determining what is spawned.
    /// Each individual table is associated with a single corresponding <see cref="ESDistributedSpawnerMarkerComponent"/> entity.
    /// </summary>
    [DataField]
    public List<EntityTableSelector> Tables = new();

    /// <summary>
    /// Scatter of entity spawn coordinates
    /// </summary>
    [DataField]
    public float Offset = 0.2f;

    /// <summary>
    /// A variable meaning whether the spawn will
    /// be able to be used again or whether
    /// it will be destroyed after the first use
    /// </summary>
    [DataField]
    public bool DeleteSpawnerAfterSpawn = true;
}
