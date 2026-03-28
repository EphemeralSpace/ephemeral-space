using Content.Shared._ES.Masks;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Troupes.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteRuleSystem))]
public sealed partial class ESParasiteConverterComponent : Component
{
    [DataField]
    public ProtoId<ESTroupePrototype> IgnoreTroupe = "ESParasite";

    [DataField]
    public ProtoId<ESMaskPrototype> Mask = "ESHost";

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("desecration");
}
