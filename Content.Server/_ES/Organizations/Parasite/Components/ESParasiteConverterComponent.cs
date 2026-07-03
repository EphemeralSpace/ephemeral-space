using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Audio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Organizations.Parasite.Components;

[RegisterComponent]
[Access(typeof(ESParasiteRuleSystem))]
public sealed partial class ESParasiteConverterComponent : Component
{
    [DataField]
    public ProtoId<ESOrganizationPrototype> IgnoreOrganization = "Parasite";

    [DataField]
    public ProtoId<ESSecretIdentityPrototype> SecretIdentity = "Host";

    [DataField]
    public SoundSpecifier Sound = new SoundCollectionSpecifier("desecration");
}
