using Content.Shared._ES.Core.Timer.Components;
using Content.Shared._ES.SecretIdentity;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.SecretIdentity.Burstworm.Components;

[RegisterComponent]
[Access(typeof(ESSecretIdentityConversionProjectileSystem))]
public sealed partial class ESSecretIdentityConversionProjectileComponent : Component
{
    [DataField]
    public ProtoId<ESOrganizationPrototype> IgnoreOrganization = "Parasite";

    [DataField]
    public ProtoId<ESSecretIdentityPrototype> SecretIdentity = "Burstworm";

    [DataField]
    public TimeSpan ConvertDelay = TimeSpan.FromSeconds(5);

    [DataField]
    public LocId Popup = "es-parasite-worm-convert";

    [DataField]
    public EntProtoId FailureTrash = "ESItemBurstwormDead";
}

public sealed partial class ESSecretIdentityConversionProjectileTimerEvent : ESEntityTimerEvent;
