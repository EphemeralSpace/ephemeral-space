using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
/// An objective that accumulates whenever a target food item is eaten
/// </summary>
[RegisterComponent]
[Access(typeof(ESEatFoodObjectiveSystem))]
public sealed partial class ESEatFoodObjectiveComponent : Component
{
    /// <summary>
    /// Valid objective options
    /// </summary>
    [DataField]
    public List<EntProtoId> Foods;

    /// <summary>
    /// Food chosen for this objective
    /// </summary>
    [DataField]
    public EntProtoId? SelectedFood;

    /// <summary>
    /// localization string for the objective title
    /// </summary>
    [DataField]
    public LocId Title = "es-eat-food-objective-title";
}
