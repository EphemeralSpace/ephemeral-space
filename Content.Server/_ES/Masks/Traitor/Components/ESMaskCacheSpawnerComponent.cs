using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Traitor.Components;

[RegisterComponent]
public sealed partial class ESMaskCacheSpawnerComponent : Component
{
    [DataField]
    public ProtoId<ESSpawnRegionPrototype> Region = "ESMaintenance";

    [DataField]
    public EntityTableSelector CacheProto = new NoneSelector();
}
