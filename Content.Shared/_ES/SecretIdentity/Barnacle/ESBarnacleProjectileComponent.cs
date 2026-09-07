using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Barnacle;

[RegisterComponent]
public sealed partial class ESBarnacleProjectileComponent : Component
{
    /// <summary>
    /// The entity who the projectile will stop when touching
    /// </summary>
    [DataField]
    public EntityUid GoalEntity;

    /// <summary>
    /// How close you can get to the entity before it counts as touching
    /// </summary>
    [DataField]
    public double Tolerance = 0.5;

    /// <summary>
    /// How fast the projectile accelarates
    /// </summary>
    [DataField]
    public float AccelerationRate = 0.05f;

    /// <summary>
    /// The entity that is spawned along the barnacles path
    /// </summary>
    [DataField]
    public EntProtoId BarnacleSpawn = "ESBarnacleTile";

    /// <summary>
    /// The entity that is spawned when the projectile touches the GoalVector
    /// </summary>
    [DataField]
    public EntProtoId BarnacleDead = "ESItemBurrowerDead";
}
