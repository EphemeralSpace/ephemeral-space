using Content.Shared._ES.SpawnRegion;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedSecretIdentityCacheSystem))]
public sealed partial class ESSecretIdentityCacheSpawnerComponent : Component
{
    [DataField]
    public ProtoId<ESSpawnRegionPrototype> Region = "ESMaintenance";

    [DataField(required: true)]
    public EntityTableSelector CacheProto;

    [DataField, AutoNetworkedField]
    public string LocationString = string.Empty;
}
