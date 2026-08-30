using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.Alert;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.SecretIdentity.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedSecretIdentityCacheSystem))]
public sealed partial class ESCeilingCacheComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MindId;

    [DataField, AutoNetworkedField]
    public EntProtoId? CacheLoot;

    [DataField]
    public ProtoId<AlertPrototype> CacheAlertProto = "ESCeilingCache";

    [DataField]
    public SoundSpecifier? RevealSound = new SoundCollectionSpecifier("storageRustle");
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedSecretIdentityCacheSystem))]
public sealed partial class ESCeilingCacheContactingComponent : Component
{
    [DataField, AutoNetworkedField]
    public List<EntityUid> Caches = new();
}

public sealed partial class ESRevealCacheAlertEvent : BaseAlertEvent;

[Serializable, NetSerializable]
public sealed partial class ESRevealCacheTimerEvent : ESEntityTimerEvent;
