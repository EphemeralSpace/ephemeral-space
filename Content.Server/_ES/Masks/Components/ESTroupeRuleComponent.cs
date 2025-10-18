using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Components;

/// <summary>
/// Handles assigning masks to players when they join into the round.
/// </summary>
[RegisterComponent]
[Access(typeof(ESMaskSystem))]
public sealed partial class ESTroupeRuleComponent : Component
{
    /// <summary>
    /// Priority for the assignment of players.
    /// Rules with equal priority will be assigned simultaneously.
    /// </summary>
    [DataField]
    public int Priority = 1;

    /// <summary>
    /// Troupe that is associated with this rule
    /// </summary>
    [DataField(required: true)]
    public ProtoId<ESTroupePrototype> Troupe;

    /// <summary>
    /// Minimum numbers of members present
    /// </summary>
    [DataField]
    public int MinTargetMembers = 1;

    /// <summary>
    /// Maximum number of members present
    /// </summary>
    [DataField]
    public int MaxTargetMembers = int.MaxValue;

    /// <summary>
    /// Proportion of players who will be selected for this troupe.
    /// Use 1 / X to convert to a percentage (i.e. value of "3" means 1/3 of players will be in this troupe)
    /// </summary>
    [DataField]
    public int PlayersPerTargetMember = 5;

    /// <summary>
    /// Minds that are a part of this troupe.
    /// </summary>
    [DataField]
    public List<EntityUid> TroupeMemberMinds = new();

    /// <summary>
    /// Objective entities this troupe has spawned
    /// </summary>
    [DataField]
    public List<EntityUid> AssociatedObjectives = new();
}
