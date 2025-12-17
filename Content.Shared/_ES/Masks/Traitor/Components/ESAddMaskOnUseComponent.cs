using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Traitor.Components;

/// <summary>
/// Adds a mask upon use of an entity
///
/// </summary>
[RegisterComponent]
public sealed partial class ESAddMaskOnUseComponent : Component
{
    [DataField]
    public bool RequireCrit = true;

    [DataField]
    public bool MindshieldPrevent = true;

    [DataField]
    public bool Used = false;

    [DataField]
    public float Delay = 5f;

    [DataField]
    public ProtoId<ESMaskPrototype> MaskToAdd;

    [DataField]
    public string UsedMessage = "subverter-chip-used";

    [DataField]
    public string UsingMessage = "subverter-chip-implanting";

    [DataField]
    public string NotUsedExamineMessage = "subverter-chip-examined-usable";

    [DataField]
    public string UsedExamineMessage = "subverter-chip-examined-used";
}
