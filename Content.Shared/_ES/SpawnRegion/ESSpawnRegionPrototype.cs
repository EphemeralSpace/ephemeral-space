using Content.Shared._ES.SpawnRegion.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SpawnRegion;

/// <summary>
/// Generic marker used in conjunction with <see cref="ESSpawnRegionMarkerComponent"/> for querying areas on the station.
/// </summary>
[Prototype("esSpawnRegion")]
public sealed partial class ESSpawnRegionPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;
}
