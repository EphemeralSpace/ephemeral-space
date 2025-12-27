using Content.Shared._ES.Objectives.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
/// This is used for a <see cref="ESObjectiveComponent"/> that's completed when a certain number of unique foods have been eaten,
/// according to <see cref="ESCounterObjectiveComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(ESEatUniqueFoodsObjectiveSystem))]
public sealed partial class ESEatUniqueFoodsObjectiveComponent : Component
{
    /// <summary>
    /// The unique food items that this mind has eaten so far.
    /// </summary>
    [DataField]
    public HashSet<EntProtoId> UniqueFoods = new();
}
