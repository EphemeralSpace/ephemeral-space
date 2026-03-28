using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Burstworm.Components;

[RegisterComponent]
[Access(typeof(ESBurstwormSystem))]
public sealed partial class ESBurstwormComponent : Component
{
    [DataField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("desecration");

    [DataField]
    public EntProtoId Projectile = "ESProjectileBurstworm";

    [DataField]
    public int ProjectileCount = 16;

    [DataField]
    public TimeSpan BurstDelay = TimeSpan.FromSeconds(1.5f);
}

public sealed partial class ESBurstwormBurstTimerEvent : ESEntityTimerEvent;
