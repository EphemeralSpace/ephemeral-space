using System.Numerics;
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
    public int MaxBarancle = 3;
}

[RegisterComponent]
public sealed partial class ESBarnacleMobComponent : Component
{
    public Entity<ESBarnacleComponent, MindComponent> BarnacleOwner;
}

[RegisterComponent]
public sealed partial class ESBarnacleProjectileComponent : Component
{
    /// <summary>
    /// The entity who the projectile will stop when touching
    /// </summary>
    public EntityUid GoalVector;

    /// <summary>
    /// How close you can get to the entity before it counts as touching
    /// </summary>
    public double Tolerance = 0.5;

    /// <summary>
    /// How fast the projectile accelarates
    /// </summary>
    public float AccelerationRate = 0.05f;

    /// <summary>
    /// The entity that is spawned along the barnacles path
    /// </summary>
    public EntProtoId BarnacleSpawn = "ESBarnacleTile";

    /// <summary>
    /// The entity that is spawned when the projectile touches the GoalVector
    /// </summary>
    public EntProtoId BarnacleDead = "ESItemBurrowerDead";
}


public sealed partial class ESBarnacleActionEvent : WorldTargetActionEvent;

[Serializable, NetSerializable]
public sealed partial class ESBarnacleDoafterEvent : SimpleDoAfterEvent
{
    [NonSerialized]
    public EntityCoordinates TargetCoord;

    [NonSerialized]
    public Entity<MindComponent, ESBarnacleComponent> Preformer;
}


[ByRefEvent]
public readonly record struct ESBarnacleDiedEvent;
