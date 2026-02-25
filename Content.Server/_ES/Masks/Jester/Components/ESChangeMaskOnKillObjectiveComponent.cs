using Content.Shared._ES.Masks;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Jester.Components;

/// <summary>
/// Used for a mask that becomes another mask when they kill someone.
/// </summary>
[RegisterComponent]
public sealed partial class ESChangeMaskOnKillObjectiveComponent : Component
{
    [DataField]
    public LocId Message = "es-fool-conversion-notification";

    [DataField(required: true)]
    public ProtoId<ESMaskPrototype> Mask;

    [DataField]
    public float DefaultProgress = 1f;
}
