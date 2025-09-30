using Content.Shared.Alert;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESCeilingCacheComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? MindId;

    [DataField, AutoNetworkedField]
    public EntProtoId? CacheLoot;

    [DataField]
    public ProtoId<AlertPrototype> CacheAlertProto;
}
