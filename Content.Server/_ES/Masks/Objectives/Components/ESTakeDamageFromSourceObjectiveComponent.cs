using Content.Shared._ES.Objectives.Components;
using Content.Shared.Tag;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
/// Objective used with <see cref="ESCounterObjectiveComponent"/> for taking a cumulative amount of damage from specific objects
/// </summary>
[RegisterComponent]
[Access(typeof(ESTakeDamageFromSourceObjectiveSystem))]
public sealed partial class ESTakeDamageFromSourceObjectiveComponent : Component
{
    /// <summary>
    ///     Different types of damages one can roll
    /// </summary>
    [DataField]
    public List<ProtoId<TagPrototype>> PossibleSources = new();

    /// <summary>
    ///     The source selected
    /// </summary>
    [DataField]
    public ProtoId<TagPrototype> SelectedSource = "ESGrille";
}
