using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SpawnRegion.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedSpawnRegionSystem))]
public sealed partial class ESSpawnRegionMarkerComponent : Component
{
    /// <summary>
    /// Corresponding area
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESSpawnRegionPrototype> Area;
}
