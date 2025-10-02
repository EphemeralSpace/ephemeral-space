using Content.Shared.Alert;
using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedMaskCacheSystem))]
public sealed partial class ESCeilingCacheComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MindId;

    [DataField, AutoNetworkedField]
    public EntProtoId? CacheLoot;

    [DataField]
    public ProtoId<AlertPrototype> CacheAlertProto = "ESCeilingCache";

    [DataField]
    public SoundSpecifier? RevealSound = new SoundPathSpecifier("/Audio/Items/toolbox_drop.ogg");
}

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedMaskCacheSystem))]
public sealed partial class ESCeilingCacheContactingComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid Cache;

    public DoAfterId? DoAfterKey;
}

public sealed partial class ESRevealCacheAlertEvent : BaseAlertEvent;
