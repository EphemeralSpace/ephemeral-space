using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Used to store all masks that a given mind has received throughout the round.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedMaskSystem))]
public sealed partial class ESMaskMemoryComponent : Component
{
    /// <summary>
    /// The masks that this mind has had, in order from oldest to newest.
    /// </summary>
    [DataField]
    public List<ProtoId<ESMaskPrototype>> Masks = [];
}
