using Content.Shared._ES.Objectives.Target.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Phantom.Components;

/// <summary>
/// Used to set up a new target for a given objective when the objective's owner gets killed.
/// </summary>
[RegisterComponent]
[Access(typeof(ESAvengeSelfObjectiveSystem))]
public sealed partial class ESAvengeSelfObjectiveComponent : Component
{
    /// <summary>
    /// Objective added when killed.
    /// </summary>
    [DataField]
    public EntProtoId<ESTargetObjectiveComponent> AvengeObjective = "ESObjectivePhantomAvenge";

    /// <summary>
    /// Message shown to player when they are successfully killed by someone.
    /// </summary>
    [DataField]
    public LocId SuccessMessage = "es-phantom-avenge-prompt-success";
}
