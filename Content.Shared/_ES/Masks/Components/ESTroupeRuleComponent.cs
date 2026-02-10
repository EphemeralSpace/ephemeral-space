using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Handles assigning masks to players when they join into the round.
/// </summary>
/// <remarks>
/// Logic only present on server.
/// </remarks>
[RegisterComponent]
[Access(typeof(ESSharedMaskSystem))]
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
    /// Minds that are a part of this troupe.
    /// </summary>
    [DataField]
    public List<EntityUid> TroupeMemberMinds = new();
}
