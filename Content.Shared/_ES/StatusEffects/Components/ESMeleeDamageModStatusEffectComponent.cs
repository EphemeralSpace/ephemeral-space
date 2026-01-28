using Robust.Shared.GameStates;

namespace Content.Shared._ES.StatusEffects.Components;

[RegisterComponent, NetworkedComponent]
public sealed partial class ESMeleeDamageModStatusEffectComponent : Component
{
    [DataField]
    public float Coefficient = 0.50f;
}
