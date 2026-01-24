using Content.Shared._ES.Objectives.Target.Components;
using Content.Shared.Actions.Components;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Avenger.Components;

/// <summary>
/// Used for a <see cref="ESTargetObjectiveComponent"/> that creates a new targeted objective which targets
/// whoever killed the current objective's target.
/// </summary>
/// <remarks>
/// Mildly convoluted.
/// </remarks>
[RegisterComponent]
[Access(typeof(ESAvengeOnKillObjectiveSystem))]
public sealed partial class ESAvengeOnKillObjectiveComponent : Component
{
    [DataField]
    public EntProtoId<ESTargetObjectiveComponent> AvengeObjective = "ESObjectiveAvengerKill";

    [DataField]
    public EntProtoId<ActionComponent> ActionPrototype = "ESActionMaskAvengerSense";
}
