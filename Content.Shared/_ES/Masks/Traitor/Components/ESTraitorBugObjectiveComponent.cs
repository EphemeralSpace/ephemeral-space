using Content.Shared.Access;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESTraitorBugSystem))]
public sealed partial class ESTraitorBugObjectiveComponent : Component
{
    [DataField]
    public LocId Title = "es-bug-objective-title";

    [DataField, AutoNetworkedField]
    public ProtoId<AccessGroupPrototype>? Target;
}
