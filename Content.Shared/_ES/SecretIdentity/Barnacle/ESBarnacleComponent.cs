using Content.Shared.Actions;
using Content.Shared.DoAfter;
using Content.Shared.Mind;
using Robust.Shared.Map;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.SecretIdentity.Barnacle;

[RegisterComponent]
public sealed partial class ESBarnacleComponent : Component
{
    [DataField]
    public List<EntityUid> Barnacles = new ();

    [DataField]
    public int MaxBarnacles = 3;
}

[RegisterComponent]
public sealed partial class ESBarnacleMobComponent : Component
{
    [DataField]
    public EntProtoId ProjectileId = "ESProjectileBarnacle";

    [DataField]
    public EntityUid BarnacleOwner;
}

[RegisterComponent]
public sealed partial class ESBarnacleProjectileComponent : Component
{
    /// <summary>
    /// The entity who the projectile will stop when touching
    /// </summary>
    [DataField]
    public EntityUid GoalVector;

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

public sealed partial class ESBarnacleActionEvent : WorldTargetActionEvent
{
    [DataField]
    public EntProtoId BarnacleEntityId = "ESBarnacle";

    [DataField]
    public TimeSpan PlantDelay = TimeSpan.FromSeconds(10);
}

// TODO: this not being serialized is kinda smelly af but i dont super care.
[Serializable, NetSerializable]
public sealed partial class ESBarnacleDoAfterEvent : SimpleDoAfterEvent
{
    [DataField]
    public EntProtoId BarnacleEntityId;

    [NonSerialized]
    public EntityCoordinates TargetCoord;

    [NonSerialized]
    public Entity<MindComponent, ESBarnacleComponent> Performer;

    [NonSerialized]
    public EntityUid Action;
}


[ByRefEvent]
public readonly record struct ESBarnacleDiedEvent;
