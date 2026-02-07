using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Protect.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESProtectTargetObjectiveSystem))]
public sealed partial class ESProtectTargetObjectiveComponent : Component
{
    /// <summary>
    /// Progress shown if the target is null
    /// </summary>
    [DataField]
    public float DefaultProgress = 1f;
}
