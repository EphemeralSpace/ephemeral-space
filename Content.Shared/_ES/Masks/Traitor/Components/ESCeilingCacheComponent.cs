using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESMaskCacheSystem))]
public sealed partial class ESCeilingCacheComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MindId;

    [DataField, AutoNetworkedField]
    public EntProtoId? CacheLoot;

    [DataField]
    public ProtoId<AlertPrototype> CacheAlertProto = "ESCeilingCache";
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESMaskCacheSystem))]
public sealed partial class ESCeilingCacheContactingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Cache;

    public DoAfterId? DoAfterKey;
}

public sealed partial class ESRevealCacheAlertEvent : BaseAlertEvent;
