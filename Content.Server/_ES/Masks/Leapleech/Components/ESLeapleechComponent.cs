using System.Linq;
using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._ES.Masks;
using Content.Shared.FixedPoint;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Leapleech.Components;

[RegisterComponent]
[Access(typeof(ESLeapleechSystem))]
public sealed partial class ESLeapleechComponent : Component
{
    [DataField]
    public Dictionary<EntityUid, FixedPoint2> LeechedEntities = new();

    [ViewVariables]
    public int LeechCount => LeechedEntities.Count(p => p.Value >= LeechDamageThreshold);

    [DataField]
    public FixedPoint2 LeechDamageThreshold = 50;

    [DataField]
    public TimeSpan BurstDelay = TimeSpan.FromSeconds(1.5f);

    [DataField]
    public SoundSpecifier BurstSound = new SoundCollectionSpecifier("desecration");

    [DataField]
    public EntProtoId Projectile = "ESMobLeepLeach";

    [DataField]
    public ProtoId<ESTroupePrototype> IgnoreTroupe = "Parasite";
}

public sealed partial class ESLeapLeechBurstTimerEvent : ESEntityTimerEvent;

