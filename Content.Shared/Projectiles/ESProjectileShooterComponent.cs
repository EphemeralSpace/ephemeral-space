using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESProjectileShooterComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Projectiles = [];
}
