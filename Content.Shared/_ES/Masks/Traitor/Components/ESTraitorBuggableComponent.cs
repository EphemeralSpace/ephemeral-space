using System.Diagnostics.CodeAnalysis;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.Access;
using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESTraitorBugSystem))]
public sealed partial class ESTraitorBuggableComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<AccessGroupPrototype> Department = "AllAccess";

    [DataField]
    public TimeSpan BugRemoveTime = TimeSpan.FromSeconds(5);

    [DataField]
    public TimeSpan BugPlantTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan BugDuration = TimeSpan.FromMinutes(5);

    [DataField, AutoNetworkedField]
    public EntityUid? Timer;

    /// <summary>
    /// Chance (per second) that the bugged entity will spark
    /// </summary>
    [DataField]
    public float BuggedSparkChance = 1f / 60;

    [ViewVariables, MemberNotNullWhen(true, nameof(Timer))]
    public bool IsBugged => Timer != null;
}

[Serializable, NetSerializable]
public sealed partial class ESPlantTraitorBugDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ESRemoveTraitorBugDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ESTraitorBugTimerEvent : ESEntityTimerEvent;

[ByRefEvent]
public readonly record struct ESTraitorBugHackedEvent(ProtoId<AccessGroupPrototype> Group);

[Serializable, NetSerializable]
public enum ESTraitorBugVisuals : byte
{
    Bugged,
}
