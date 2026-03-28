using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Troupes.Parasite.Components;

/// <summary>
/// An objective that is measured by the number of living troupe over the number of other troupe members.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTroupeOutnumberObjectiveSystem))]
public sealed partial class ESTroupeOutnumberObjectiveComponent : Component
{
    [DataField]
    public ProtoId<ESTroupePrototype> Troupe;

    [DataField]
    public float TargetPercentage = 0.5f;
}
