using Content.Shared._ES.Masks;
using Content.Shared.Trigger.Components.Effects;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Trigger.Component;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESChangeMaskOnTriggerComponent : BaseXOnTriggerComponent
{
    [DataField]
    public ProtoId<ESMaskPrototype> Mask;

    // Do we want to be able to convert masks into the same mask they already are?
    [DataField]
    public bool SameTroupeConversion;
}
