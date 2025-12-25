using Content.Shared._ES.Masks;
using Content.Shared._ES.Masks.Components;
using Content.Shared.EntityEffects;
using Robust.Shared.GameStates;
using Robust.Shared.Player;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Traitor.Components;

/// <summary>
/// Adds a mask upon use of an entity
///
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESAddMaskOnUseComponent : Component
{
    [DataField]
    public bool RequireCrit = true;

    [DataField]
    public bool MindshieldPrevent = true;

    [DataField, AutoNetworkedField]
    public bool Used = false;

    [DataField]
    public float Delay = 5f;

    [DataField]
    public ProtoId<ESMaskPrototype> MaskToAdd;

    [DataField]
    public LocId UsedMessage = "subverter-chip-used";

    [DataField]
    public LocId UsingMessage = "subverter-chip-implanting";

    [DataField]
    public LocId NotUsedExamineMessage = "subverter-chip-examined-usable";

    [DataField]
    public LocId UsedExamineMessage = "subverter-chip-examined-used";

    [DataField]
    public bool RemovePreviousMask = true;
}
