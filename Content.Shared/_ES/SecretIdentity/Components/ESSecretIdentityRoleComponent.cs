using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Component used to identify a mind as having a specific mask.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
[Access(typeof(ESSharedSecretIdentitySystem))]
public sealed partial class ESSecretIdentityRoleComponent : Component
{
    /// <summary>
    /// The mask corresponding to this role entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ESSecretIdentityPrototype>? Mask;

    /// <summary>
    /// Actions added to the entity from the mask.
    /// </summary>
    [DataField]
    public List<EntityUid> Actions = new();
}
