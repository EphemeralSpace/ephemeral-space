using Content.Shared.Humanoid.Markings;
using Robust.Shared.Prototypes;

namespace Content.Shared.Humanoid.Prototypes;

public sealed partial class SpeciesPrototype : IPrototype
{
    [DataField]
    public List<ProtoId<MarkingPrototype>> MaleHair = new();

    [DataField]
    public List<ProtoId<MarkingPrototype>> FemaleHair = new();

    [DataField]
    public List<ProtoId<MarkingPrototype>> UnisexHair = new();
}
