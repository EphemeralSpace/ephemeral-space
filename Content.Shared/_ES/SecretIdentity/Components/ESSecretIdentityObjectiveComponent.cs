using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Marker component.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESSecretIdentityObjectiveComponent : Component
{
    [DataField, AutoNetworkedField]
    public ProtoId<ESSecretIdentityPrototype>? AssociatedIdentity;

    /// <summary>
    /// If true, objective will remain despite secret identity changes.
    /// </summary>
    [DataField]
    public bool TransferBetweenIdentities;
}
