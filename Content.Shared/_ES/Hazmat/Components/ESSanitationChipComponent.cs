using Robust.Shared.GameStates;
using Robust.Shared.Serialization;
using Robust.Shared.Prototypes;
using Content.Shared.DoAfter;
using Content.Shared._ES.Hazmat;
using Content.Shared.Chemistry.Components;

namespace Content.Shared._ES.Hazmat.Components;

[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(ESSharedSanitationChipSystem))]
public sealed partial class ESSanitationChipComponent : Component
{
    // todo datafield for the foam? it might just be the prototype id?
    [DataField]
    public float MovementThreshold = 0.1f;

    [DataField, AutoNetworkedField]
    public TimeSpan DelayTime = TimeSpan.FromSeconds(15);

    [DataField, AutoNetworkedField]
    public TimeSpan Duration = TimeSpan.FromSeconds(30);

    [DataField(required: true), AutoNetworkedField]
    public int SpreadAmount;

    [DataField, AutoNetworkedField]
    public EntProtoId SmokePrototype = "RemoveGasFoam";

    [DataField, AutoNetworkedField]
    public Solution Solution = new();
}

[Serializable, NetSerializable]
public sealed partial class ESSanitationChipDoAfterEvent : SimpleDoAfterEvent;
