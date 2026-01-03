namespace Content.Server._ES.Masks.Phantom.Components;

/// <summary>
/// Used to set up a new target for a given objective when the objective's owner gets killed.
/// </summary>
[RegisterComponent]
[Access(typeof(ESAvengeSelfObjectiveSystem))]
public sealed partial class ESAvengeSelfObjectiveComponent : Component
{
    /// <summary>
    /// Objective name used when the player fails to be killed by someone.
    /// </summary>
    [DataField]
    public LocId FailName = "es-phantom-avenge-objective-fail";

    /// <summary>
    /// Message shown to player when they fail to be killed by someone.
    /// </summary>
    [DataField]
    public LocId FailMessage = "es-phantom-avenge-prompt-fail";

    /// <summary>
    /// Message shown to player when they are successfully killed by someone.
    /// </summary>
    [DataField]
    public LocId SuccessMessage = "es-phantom-avenge-prompt-success";
}
