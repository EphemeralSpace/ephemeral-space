using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedMaskCacheSystem))]
public sealed partial class ESMaskCacheSpawnerComponent : Component
{
    [DataField]
    public ProtoId<ESSpawnRegionPrototype> Region = "ESMaintenance";

    [DataField(required: true)]
    public EntityTableSelector CacheProto;

    [DataField, AutoNetworkedField]
    public string LocationString = string.Empty;
}
