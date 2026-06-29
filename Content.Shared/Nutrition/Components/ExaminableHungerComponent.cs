using Content.Shared.Nutrition.EntitySystems;
using Robust.Shared.GameStates;

namespace Content.Shared.Nutrition.Components;

/// <summary>
/// Adds text to the entity's description box based on its current hunger threshold.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ExaminableHungerSystem))]
public sealed partial class ExaminableHungerComponent : Component
{
    /// <summary>
    /// Dictionary of hunger thresholds to LocIds of the messages to display.
    /// </summary>
    [DataField]
    public Dictionary<HungerThreshold, LocId> Descriptions = new()
    {
        { HungerThreshold.Okay, "examinable-hunger-component-examine-okay"},
        { HungerThreshold.Peckish, "examinable-hunger-component-examine-peckish"},
        { HungerThreshold.Hungry, "examinable-hunger-component-examine-hungry"},
        { HungerThreshold.Starving, "examinable-hunger-component-examine-starving"}
    };

    /// <summary>
    /// LocId of a fallback message to display if the entity has no <see cref="HungerComponent"/>
    /// or does not have a value in <see cref="Descriptions"/> for the current threshold.
    /// </summary>
    public LocId NoHungerDescription = "examinable-hunger-component-examine-none";
}
