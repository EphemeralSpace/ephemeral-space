namespace Content.Server._ES.StationEvents.ElectricalOverload.Components;

/// <summary>
///     Event which will pick a few APCs in some radius, and break random lights around them, causing fires.
/// </summary>
[RegisterComponent]
[Access(typeof(ESElectricalOverloadRule))]
public sealed partial class ESElectricalOverloadRuleComponent : Component
{
    [DataField]
    public List<EntityUid> Apcs = [];

    [DataField]
    public float Radius = 7f;

    /// <summary>
    ///     Radius to create fires in around each APC
    /// </summary>
    /// <remarks>
    ///     Fires will also naturally start from the lights breaking, so be a little conservative
    /// </remarks>
    [DataField]
    public float FireRadius = 3f;

    /// <summary>
    /// % of fire circle that will spawn flames
    /// </summary>
    [DataField]
    public float FireChance = 0.6f;
}
