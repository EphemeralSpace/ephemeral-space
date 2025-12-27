using Robust.Shared.Map;

namespace Content.Server._ES.StationEvents.ElectricalFire.Components;

/// <summary>
/// This is used for a random event that spawns a fire somewhere on the station.
/// </summary>
[RegisterComponent]
[Access(typeof(ESElectricalFireRule))]
public sealed partial class ESElectricalFireRuleComponent : Component
{
    /// <summary>
    /// Where teh fire will start
    /// </summary>
    [DataField]
    public EntityCoordinates? TargetCoordinates;

    /// <summary>
    /// Radius of the fire circle
    /// </summary>
    [DataField]
    public float FireRadius = 3f;

    /// <summary>
    /// % of fire circle that will spawn flames
    /// </summary>
    [DataField]
    public float FireChance = 0.75f;
}
