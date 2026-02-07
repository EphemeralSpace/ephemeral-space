using Content.Shared.Whitelist;

namespace Content.Server._ES.Masks.Secretary.Components;

/// <summary>
/// This is a component for an objective which succeeds if your target completes their objectives.
/// So it's like and objective referencing itself? it's hard to describe.
/// Fuck the world.
/// 10,000,000,000 bombs dropped.
/// Love is violence.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetCompleteObjectivesSystem))]
public sealed partial class ESTargetCompleteOwnedObjectiveComponent : Component
{
    /// <summary>
    /// Objectives that will be blacklisted and ignored for the purposes of this objective.
    /// Useful to prevent horrific infinity loops like a secretary targeting another secretary.
    /// </summary>
    [DataField]
    public EntityWhitelist? ObjectiveBlacklist;

    [DataField]
    public float DefaultProgress;
}
