using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fibers.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESFiberSystem))]
public sealed partial class ESFiberClothingComponent : Component
{
    /// <summary>
    /// A description of the physical characteristics of the fiber.
    /// Typically just a color, but can also denote texture or unique materials.
    /// </summary>
    [DataField, AutoNetworkedField]
    public LocId? PhysicalDescription;

    /// <summary>
    /// Unique identifiers per clothing item. A definitive source of "truth" for comparing clothing fibers.
    /// </summary>
    [DataField, AutoNetworkedField]
    public int FiberId;

    [DataField]
    public float TransferChance = 0.2f;
}

[Serializable, NetSerializable]
[DataDefinition]
public partial record struct ESFiber(string Appearance, int Id)
{
    [DataField]
    public string Appearance = Appearance;

    [DataField]
    public int Id = Id;
}
