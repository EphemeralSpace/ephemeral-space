using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
///     Sets counter to 1 if the owner was killed by someone else--not just any death.
/// </summary>
[RegisterComponent]
public sealed partial class ESBeKilledObjectiveComponent : Component
{
    /// <summary>
    ///     If non-null, the killer must be of this troupe in order to count as a completion.
    /// </summary>
    [DataField]
    public ProtoId<ESTroupePrototype>? TroupeRequired = null;
}
