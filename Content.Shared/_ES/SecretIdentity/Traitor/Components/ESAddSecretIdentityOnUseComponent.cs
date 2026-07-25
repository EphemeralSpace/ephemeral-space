using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.SecretIdentity.Traitor.Components;

/// <summary>
/// Adds a secret identity upon use of an entity
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESAddSecretIdentityOnUseSystem))]
public sealed partial class ESAddSecretIdentityOnUseComponent : Component
{
    /// <summary>
    /// Whether the target must be in crit to be converted
    /// </summary>
    [DataField]
    public bool RequireIncapacitated = true;

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
    /// The secret identity the target will be given
    /// </summary>
    [DataField]
    public ProtoId<ESSecretIdentityPrototype> SecretIdentityToAdd;

    [DataField]
    public LocId StagehandNotification = "es-subverter-chip-announcement";

    [DataField]
    public LocId UsedMessage = "es-subverter-chip-used";

    [DataField]
    public LocId UsingMessage = "es-subverter-chip-implanting";

    [DataField]
    public LocId NotUsedExamineMessage = "es-subverter-chip-examined-usable";

    [DataField]
    public LocId UsedExamineMessage = "es-subverter-chip-examined-used";

    [DataField]
    public LocId NotIncapacitatedMessage = "es-subverter-chip-not-crit";

    [DataField]
    public LocId MindshieldedMessage = "es-subverter-chip-mindshielded";
}
