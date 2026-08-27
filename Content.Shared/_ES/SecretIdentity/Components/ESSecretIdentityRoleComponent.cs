using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Component used to identify a mind as having a specific secret identity.
/// </summary>
[RegisterComponent, NetworkedComponent]
[AutoGenerateComponentState(true)]
[Access(typeof(ESSharedSecretIdentitySystem))]
public sealed partial class ESSecretIdentityRoleComponent : Component
{
    /// <summary>
    /// The secret identity corresponding to this role entity
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ESSecretIdentityPrototype>? SecretIdentity;

    /// <summary>
    /// Modifier applied to this secret identity.
    /// </summary>
    [DataField, AutoNetworkedField]
    public ProtoId<ESSecretIdentityModifierPrototype>? Modifier;

    /// <summary>
    /// Actions added to the entity from the secret identity.
    /// </summary>
    [DataField]
    public List<EntityUid> Actions = new();
}
