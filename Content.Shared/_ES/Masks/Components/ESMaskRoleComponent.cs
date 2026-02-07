using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Components;

/// <summary>
/// Component used to identify a mind as having a specific mask.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
[Access(typeof(ESSharedMaskSystem))]
public sealed partial class ESMaskRoleComponent : Component
{
    /// <summary>
    /// The mask corresponding to this role entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ESMaskPrototype>? Mask;
}
