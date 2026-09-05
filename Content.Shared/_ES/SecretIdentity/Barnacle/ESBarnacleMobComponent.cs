using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Barnacle;

[RegisterComponent]
public sealed partial class ESBarnacleMobComponent : Component
{
    [DataField]
    public EntProtoId ProjectileId = "ESProjectileBarnacle";

    [DataField]
    public EntityUid BarnacleOwner;
}
