using Content.Shared._ES.Objectives.Target.Components;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Used with <see cref="ESTargetObjectiveComponent"/> to filter based on a player's troupe
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetTroupeObjectiveSystem))]
public sealed partial class ESTargetTroupeObjectiveComponent : Component
{
    /// <summary>
    /// The troupe that must be present on the player
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESTroupePrototype> Troupe;

    /// <summary>
    /// If true, will select if a given player does NOT have <see cref="Troupe"/> as their troupe
    /// </summary>
    [DataField]
    public bool Invert;

    /// <summary>
    /// If a given player has any mask in this set, they will NOT be invalidated, even if they otherwise would be.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESMaskPrototype>> OverrideMasks = new();
}
