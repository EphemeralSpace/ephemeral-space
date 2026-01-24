using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Traitor.Components;

/// <summary>
/// Adds a mask upon use of an entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESAddMaskOnUseSystem))]
public sealed partial class ESAddMaskOnUseComponent : Component
{
    /// <summary>
    /// Whether the target must be in crit to be converted
    /// </summary>
    [DataField]
    public bool RequireCrit = true;

    /// <summary>
    /// Whether having a mindshield will prevent conversion
    /// </summary>
    [DataField]
    public bool MindshieldPrevent = true;

    /// <summary>
    /// If this has been used.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Used;

    /// <summary>
    /// Time it takes to convert
    /// </summary>
    [DataField]
    public float Delay = 5f;

    /// <summary>
    /// The mask the target will be given
    /// </summary>
    [DataField]
    public ProtoId<ESMaskPrototype> MaskToAdd;

    [DataField]
    public LocId UsedMessage = "es-subverter-chip-used";

    [DataField]
    public LocId UsingMessage = "es-subverter-chip-implanting";

    [DataField]
    public LocId NotUsedExamineMessage = "es-subverter-chip-examined-usable";

    [DataField]
    public LocId UsedExamineMessage = "es-subverter-chip-examined-used";

    [DataField]
    public LocId NotCritMessage = "es-subverter-chip-not-crit";

    [DataField]
    public LocId MindshieldedMessage = "es-subverter-chip-mindshielded";
}
