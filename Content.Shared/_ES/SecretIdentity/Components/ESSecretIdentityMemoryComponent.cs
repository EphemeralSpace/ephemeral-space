using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
/// Used to store all secret identities that a given mind has received throughout the round.
/// </summary>
[RegisterComponent]
[Access(typeof(ESSharedSecretIdentitySystem))]
public sealed partial class ESSecretIdentityMemoryComponent : Component
{
    /// <summary>
    /// The secret identities that this mind has had, in order from oldest to newest.
    /// </summary>
    [DataField]
    public List<ProtoId<ESSecretIdentityPrototype>> SecretIdentities = [];
}
