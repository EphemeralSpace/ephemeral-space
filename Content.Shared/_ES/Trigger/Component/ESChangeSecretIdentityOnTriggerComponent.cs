using Content.Shared._ES.SecretIdentity;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Trigger.Component;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESChangeSecretIdentityOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public ProtoId<ESSecretIdentityPrototype> SecretIdentity;

    // Do we want to be able to convert secret identities into the same faction they already are?
    [DataField]
    public bool SameOrganizationConversion;
}
