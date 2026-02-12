using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Component that tracks the last mask that a particular body had.
/// If a mind with a mask inhabits a body, this component will be updated to store that information.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedMaskSystem))]
public sealed partial class ESBodyLastMaskComponent : Component
{
    /// <summary>
    /// The last mask that this body had.
    /// </summary>
    [DataField]
    public ProtoId<ESMaskPrototype> LastMask;
}
