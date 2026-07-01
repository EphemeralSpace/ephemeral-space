using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Hemophage.Components;

[RegisterComponent]
[Access(typeof(ESSecretIdentityConvertOnCollideSystem))]
public sealed partial class ESSecretIdentityConvertOnCollideComponent : Component
{
    [DataField]
    public ProtoId<ESTroupePrototype> IgnoreTroupe = "Parasite";

    [DataField]
    public ProtoId<ESSecretIdentityPrototype> SecretIdentity = "Hemophage";
}
