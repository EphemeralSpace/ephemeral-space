using Robust.Shared.GameStates;

namespace Content.Shared.Projectiles;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESProjectileWeaponComponent : Component
{
    [DataField, AutoNetworkedField]
    public HashSet<EntityUid> Projectiles = [];
}
