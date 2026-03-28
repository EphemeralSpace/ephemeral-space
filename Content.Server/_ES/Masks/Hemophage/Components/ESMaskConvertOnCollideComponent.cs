using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Hemophage.Components;

[RegisterComponent]
[Access(typeof(ESMaskConvertOnCollideSystem))]
public sealed partial class ESMaskConvertOnCollideComponent : Component
{
    [DataField]
    public ProtoId<ESTroupePrototype> IgnoreTroupe = "ESParasite";

    [DataField]
    public ProtoId<ESMaskPrototype> Mask = "ESHemophage";
}
