using Content.Shared._ES.Objectives.Target.Components;
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
    /// The mind of our <see cref="ESTargetObjectiveComponent"/> target.
    /// We store this because we want to be able to keep tracking objectives if our target dies.
    /// The objective itself can't target the mind because if our guy mind swaps, we want to be able to
    /// update to the new target's objectives, for what i'll broadly call "humor" reasons.
    /// </summary>
    [DataField]
    public EntityUid? TargetMind;

    /// <summary>
    /// Objectives that will be blacklisted and ignored for the purposes of this objective.
    /// Useful to prevent horrific infinity loops like a secretary targeting another secretary.
    /// </summary>
    [DataField]
    public EntityWhitelist? ObjectiveBlacklist;

    [DataField]
    public float DefaultProgress;

    /// <summary>
    /// If true, will invert the progress. So completing objectives will make it go down, rather than up.
    /// </summary>
    [DataField]
    public bool Invert;
}
