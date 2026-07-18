using Content.Shared.Damage.Systems;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Shared.Damage.Components;

/// <summary>
/// Should the entity take damage / be stunned if colliding at a speed above MinimumSpeed?
/// </summary>
[RegisterComponent, NetworkedComponent, Access(typeof(DamageOnHighSpeedImpactSystem))]
public sealed partial class DamageOnHighSpeedImpactComponent : Component
{
    [DataField("minimumSpeed"), ViewVariables(VVAccess.ReadWrite)]
    public float MinimumSpeed = 5.5f;

    [DataField("speedDamageFactor"), ViewVariables(VVAccess.ReadWrite)]
    public float SpeedDamageFactor = 0.5f;

    [DataField(required: true)]
    public SoundSpecifier SoundHit = default!;

    [DataField]
    public int StunMinimumDamage = 10;

    [DataField, ViewVariables(VVAccess.ReadWrite)]
    public TimeSpan StunTime = TimeSpan.FromSeconds(3f);

    [DataField]
    public TimeSpan DamageCooldown = TimeSpan.FromSeconds(3f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer))]
    public TimeSpan? LastHit;

    [DataField(required: true)]
    public DamageSpecifier Damage = default!;
}
