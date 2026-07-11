using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Barricade.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESBarricadeKitSystem))]
public sealed partial class ESBarricadeKitComponent : Component
{
    [DataField]
    public EntProtoId AirlockBarricade = "BarricadeBlock";

    [DataField]
    public TimeSpan SetupDelay = TimeSpan.FromSeconds(3f);
}

[Serializable, NetSerializable]
public sealed partial class ESBarricadeAirlockDoAfterEvent : SimpleDoAfterEvent;
