using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._Offbrand.StatusEffects;

[RegisterComponent]
[Access(typeof(DisruptOnAttackStatusEffectSystem))]
public sealed partial class DisruptOnAttackStatusEffectComponent : Component
{
    [DataField]
    public EntProtoId PainStunStatusEffect = "StatusEffectPainStun";

    [DataField]
    public EntProtoId CooldownStatusEffect = "StatusEffectPainStunCooldown";

    [DataField]
    public TimeSpan PainStunDuration = TimeSpan.FromSeconds(6);

    [DataField]
    public TimeSpan CooldownDuration = TimeSpan.FromSeconds(3);
}

[RegisterComponent, NetworkedComponent]
[Access(typeof(DisruptOnAttackStatusEffectSystem))]
public sealed partial class ESPainStunCooldownStatusEffectComponent : Component;
