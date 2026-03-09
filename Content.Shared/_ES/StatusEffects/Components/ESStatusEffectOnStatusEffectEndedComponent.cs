using Content.Shared.StatusEffectNew.Components;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.StatusEffects.Components;

/// <summary>
///     Adds a new status effect to an entity when one ends. Allows chaining effects
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESStatusEffectOnStatusEffectEndedComponent : Component
{
    /// <summary>
    ///     Status effect to add when ending
    /// </summary>
    [DataField(required: true)]
    public EntProtoId<StatusEffectComponent> StatusEffect = default!;

    [DataField(required: true)]
    public TimeSpan Duration;

    [DataField]
    public TimeSpan? Delay;
}
