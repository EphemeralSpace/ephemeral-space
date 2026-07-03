using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Components;

/// <summary>
///     This is used for blacklisting certain secret identities from targeted objectives.
/// </summary>
[RegisterComponent]
public sealed partial class ESTargetSecretIdentityBlacklistComponent : Component
{
    /// <summary>
    /// A blacklist of secret identities that cannot be targeted.
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESSecretIdentityPrototype>> SecretIdentityBlacklist;
}
