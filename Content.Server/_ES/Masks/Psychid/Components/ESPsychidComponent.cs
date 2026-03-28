using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Psychid.Components;

[RegisterComponent]
[Access(typeof(ESPsychidSystem))]
public sealed partial class ESPsychidComponent : Component
{
    [DataField]
    public EntityUid? KillerMind;

    /// <summary>
    /// Troupe that, if the killer, will not cause the effect to occur.
    /// </summary>
    [DataField]
    public ProtoId<ESTroupePrototype> IgnoredTroupe = "ESParasite";
}
