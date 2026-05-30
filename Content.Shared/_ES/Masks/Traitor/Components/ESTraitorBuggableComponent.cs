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
    public TimeSpan BugPlantTime = TimeSpan.FromSeconds(3);

    [DataField]
    public TimeSpan BugDuration = TimeSpan.FromMinutes(1);

    [DataField, AutoNetworkedField]
    public EntityUid? Timer;

    [ViewVariables]
    public bool IsBugged => Timer != null;
}

[Serializable, NetSerializable]
public sealed partial class ESPlantTraitorBugDoAfterEvent : SimpleDoAfterEvent;

[Serializable, NetSerializable]
public sealed partial class ESTraitorBugTimerEvent : ESEntityTimerEvent;
