using Content.Shared.Chemistry.Reagent;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
///     This contains data for the "imbibe unique reagents" objective, which requires the engager to drink
///     some units of that reagent.
///     The number of reagents that need to be tried is determined by <see cref="NumberObjectiveComponent"/>.
/// </summary>
/// <seealso cref="ESImbibeReagentObjectiveSystem"/>
[RegisterComponent]
[Access(typeof(ESImbibeUniqueReagentsObjectiveSystem))]
public sealed partial class ESImbibeUniqueReagentsObjectiveComponent : Component
{
    /// <summary>
    ///     The reagents that have been seen so far by this objective.
    /// </summary>
    [DataField]
    public HashSet<ReagentId> SeenReagents = new();

    /// <summary>
    ///     Whether we count consuming it from food, and not just drinking.
    /// </summary>
    [DataField(required: true)]
    public bool CanBeFromFood  { get; private set; }
}
