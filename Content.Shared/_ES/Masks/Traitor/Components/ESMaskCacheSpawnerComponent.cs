using Content.Shared._ES.SpawnRegion;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent]
[Access(typeof(ESSharedMaskCacheSystem))]
public sealed partial class ESMaskCacheSpawnerComponent : Component
{
    [DataField]
    public ProtoId<ESSpawnRegionPrototype> Region = "ESMaintenance";

    [DataField(required: true)]
    public EntProtoId CacheProto;

    [DataField]
    public string LocationBriefing = string.Empty;
}
