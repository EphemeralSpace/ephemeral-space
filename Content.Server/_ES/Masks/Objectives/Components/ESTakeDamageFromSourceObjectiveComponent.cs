using Content.Shared.FixedPoint;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

[RegisterComponent]
[Access(typeof(ESTakeDamageFromSourceObjectiveSystem))]
public sealed partial class ESTakeDamageFromSourceObjectiveComponent : Component
{
    /// <summary>
    ///     Diffrent types of damages one can roll
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> PossibleSources = new();

    /// <summary>
    ///     The source selected
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> SelectedSource = "ESGrille";
}
