using Content.Shared.Actions;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.MaskCycle;

/// <summary>
///     An action event for changing to another mask.
/// </summary>
public sealed partial class ESActionChangeMaskEvent : InstantActionEvent
{
    [DataField(required: true)]
    public ProtoId<ESMaskPrototype> Mask;
}
