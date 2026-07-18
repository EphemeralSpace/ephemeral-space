using Content.Shared.Whitelist;

namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// Variant of <see cref="ESTargetObjectiveComponent"/> that takes the target from another objective on the same holder.
/// </summary>
[RegisterComponent]
[Access(typeof(ESShareTargetObjectiveSystem))]
public sealed partial class ESShareTargetObjectiveComponent : Component
{
    /// <summary>
    /// Whitelist that determines which objective will be used for the shared target.
    /// </summary>
    [DataField]
    public EntityWhitelist ObjectiveWhitelist;
}
