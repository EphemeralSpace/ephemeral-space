using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Leapleech.Components;

[RegisterComponent]
public sealed partial class ESLeapleechComponent : Component
{
    [DataField]
    public List<EntityUid> LeechedEntities = new List<EntityUid>();

    [DataField]
    public int LeechCount = 0;

    [DataField]
    public TimeSpan BurstDelay = TimeSpan.FromSeconds(1.5f);

    [DataField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("desecration");

    [DataField]
    public EntProtoId Projectile = "ESMobLeepLeach";
}

public sealed partial class ESLeapLeechBurstTimerEvent : ESEntityTimerEvent;

