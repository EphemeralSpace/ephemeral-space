using Robust.Shared.GameStates;

namespace Content.Shared._ES.Food;

/// <summary>
///     ES has a custom food system, not relying on reagent satiation/nutriment etc
///     Hunger is simplified into discrete integer levels, and 1 portion of food = 1 less point of hunger, basically
///     We hook into ingestion events to handle things like not removing reagent, and actual satiation
/// </summary>
/// <remarks>
///     wrt reagents, essentially the reagents contained in the food are reapplied every time and just not removed
///     so the reagents contained in the food are what will be consumed every bite, balance around that accordingly
/// </remarks>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESFoodComponent : Component
{
    /// <summary>
    ///     The amount of portions this piece of food starts with.
    ///     Each portion of food reduces hunger by 1.
    /// </summary>
    [DataField("portions"), AutoNetworkedField]
    public int StartingPortions = 1;

    /// <summary>
    ///     The actual amount of portions left.
    ///     Null if the food hasn't been eaten at all yet (i.e. it's at <see cref="StartingPortions"/>).
    /// </summary>
    [DataField, AutoNetworkedField]
    public int? PortionsLeft;

    /// <summary>
    ///     Multiplier on satiety per portion.
    ///     You really should not be changing this in most cases--just increase the number of portions instead
    ///     Only in the cases of much stronger food or food which should have a negative effect should you change this
    /// </summary>
    [DataField, AutoNetworkedField]
    public int SatietyMultiplier = 1;
}
