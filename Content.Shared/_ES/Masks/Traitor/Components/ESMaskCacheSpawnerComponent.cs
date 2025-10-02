using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent]
[Access(typeof(ESMaskCacheSystem))]
public sealed partial class ESMaskCacheSpawnerComponent : Component
{
    [DataField]
    public ProtoId<ESSpawnRegionPrototype> Region = "ESMaintenance";

    [DataField(required: true)]
    public EntProtoId CacheProto;
}
