using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.StatusEffects.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESAddStatusEffectOnRemovedStatusEffectComponent : Component
{
    [DataField(required: true)]
    public EntProtoId<StatusEffectComponent> StatusEffect;

    [DataField(required: true)]
    public TimeSpan Duration;
}
