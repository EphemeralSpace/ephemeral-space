using Content.Shared.FixedPoint;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Hemophage.Components;

[RegisterComponent]
[Access(typeof(ESHemophageSystem))]
public sealed partial class ESHemophageComponent : Component
{
    /// <summary>
    /// Amount of blood needed in a puddle for effects to occur.
    /// </summary>
    [DataField]
    public FixedPoint2 BloodThreshold = 0;

    [DataField]
    public EntProtoId PuddleSpawn = "ESHemophagePile";
}
