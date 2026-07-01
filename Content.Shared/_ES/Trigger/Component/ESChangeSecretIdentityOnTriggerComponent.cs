using Content.Shared._ES.SecretIdentity;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Trigger.Component;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESChangeSecretIdentityOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public ProtoId<ESSecretIdentityPrototype> Mask;

    // Do we want to be able to convert masks into the same mask they already are?
    [DataField]
    public bool SameTroupeConversion;
}
