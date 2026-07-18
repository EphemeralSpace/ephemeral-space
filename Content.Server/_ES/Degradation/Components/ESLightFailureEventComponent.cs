namespace Content.Server._ES.Degradation.Components;

/// <summary>
///     A degradation event which will randomly choose a couple lights to mess with
///     This includes breaking them atm but will ideally include flickering etc later.
/// </summary>
[RegisterComponent]
public sealed partial class ESLightFailureEventComponent : Component
{
    /// <summary>
    ///     Minimum lights to mess with
    /// </summary>
    /// <remarks>
    ///     This seems like a lot but ime this is how many you have to break considering how often this runs
    ///     for it to actually be meaningful
    /// </remarks>
    [DataField]
    public int MinCount = 10;

    /// <summary>
    ///     Maximum lights to mess with
    /// </summary>
    [DataField]
    public int MaxCount = 15;

    /// <summary>
    ///     Chance for lights broken to tilefire. This is usually 5% but gets lowered here for obvious reasons
    /// </summary>
    [DataField]
    public float TileFireChanceOverride = 0.0075f;
}
