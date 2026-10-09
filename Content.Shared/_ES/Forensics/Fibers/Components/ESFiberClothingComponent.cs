using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Fibers.Components;

/// <summary>
/// Denotes an article of clothing capable of transferring fibers on interaction to entities when worn by someone.
/// </summary>
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

    /// <summary>
    /// Chance per interaction that the entity transfers a fiber to the interacted object.
    /// </summary>
    [DataField]
    public float TransferChance = 0.2f;
}

/// <summary>
/// Representation of a unique clothing fiber
/// </summary>
[Serializable, NetSerializable]
[DataDefinition]
public partial record struct ESFiber(string Appearance, int Id)
{
    [DataField]
    public string Appearance = Appearance;

    [DataField]
    public int Id = Id;
}
