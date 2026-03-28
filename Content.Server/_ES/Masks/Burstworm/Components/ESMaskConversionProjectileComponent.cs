using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Burstworm.Components;

[RegisterComponent]
[Access(typeof(ESMaskConversionProjectileSystem))]
public sealed partial class ESMaskConversionProjectileComponent : Component
{
    [DataField]
    public ProtoId<ESTroupePrototype> IgnoreTroupe = "ESParasite";

    [DataField]
    public ProtoId<ESMaskPrototype> Mask = "ESBurstworm";

    [DataField]
    public TimeSpan ConvertDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public LocId Popup = "es-parasite-worm-convert";

    [DataField]
    public EntProtoId FailureTrash = "ESItemBurstwormDead";
}

public sealed partial class ESMaskConversionProjectileTimerEvent : ESEntityTimerEvent;
