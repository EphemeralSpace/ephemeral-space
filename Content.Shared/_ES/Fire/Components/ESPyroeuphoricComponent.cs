using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Fire.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESPyroeuphoricSystem))]
public sealed partial class ESPyroeuphoricComponent : Component
{
    [DataField]
    public EntProtoId StatusEffect = "ESStatusEffectEmotionPyroeuphoria";
}
