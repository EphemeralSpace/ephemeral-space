using Content.Shared._ES.Objectives.Components;
using Content.Shared.Whitelist;

namespace Content.Shared._ES.Masks.Traitor.Components;

/// <summary>
/// A <see cref="ESObjectiveComponent"/> condition for sabotaging a particular object with <see cref="ESSabotageTargetComponent"/>.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSabotageConditionSystem))]
public sealed partial class ESSabotageConditionComponent : Component
{
    /// <summary>
    /// Whitelist that determines if a given sabotaged entity is valid for this objective.
    /// </summary>
    [DataField(required: true)]
    public EntityWhitelist Whitelist = new();
}
