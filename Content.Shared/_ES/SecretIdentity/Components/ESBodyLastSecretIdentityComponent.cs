using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Component that tracks the last secret identity that a particular body had.
/// If a mind with a secret identity inhabits a body, this component will be updated to store that information.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedSecretIdentitySystem))]
public sealed partial class ESBodyLastSecretIdentityComponent : Component
{
    /// <summary>
    /// The last secret identity that this body had.
    /// </summary>
    [DataField]
    public ProtoId<ESSecretIdentityPrototype> LastSecretIdentity;
}
