using Content.Shared._ES.SecretIdentity;
using Content.Shared._ES.SecretIdentity.Masquerades;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Superfan;

/// <summary>
/// Used for a secret identity that converts to a separate set of
/// secret identities when one of their organization members are killed or converted.
/// </summary>
[RegisterComponent]
public sealed partial class ESSuperfanComponent : Component
{
    [DataField(required: true)]
    public ProtoId<ESOrganizationPrototype> TargetOrganization;

    [DataField(required: true)]
    public MasqueradeEntry? TargetSecretIdentity;
}
