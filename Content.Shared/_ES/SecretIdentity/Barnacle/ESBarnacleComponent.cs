using Content.Shared.Actions;
using Content.Shared.Alert;
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
    public ProtoId<AlertPrototype> BarnacleAlert = "ESBarnacleCounter";

    [DataField]
    public List<EntityUid> Barnacles = new ();

    [DataField]
    public int MaxBarnacles = 3;
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

[Serializable, NetSerializable]
public enum ESBarnacleVisuals : byte
{
    Popped,
}
