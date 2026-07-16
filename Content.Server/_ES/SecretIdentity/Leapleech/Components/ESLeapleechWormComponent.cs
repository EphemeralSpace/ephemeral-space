using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Leapleech.Components;

[RegisterComponent]
[Access(typeof(ESLeapleechSystem))]
public sealed partial class ESLeapleechWormComponent : Component
{
    [DataField]
    public ProtoId<ESSecretIdentityPrototype> Identity = "Leapleech";

    [DataField]
    public ProtoId<ESOrganizationPrototype> IgnoreOrganization = "Parasite";
}
