using Content.Shared.Radio;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Masks.Survivalist;

/// <summary>
///     Handles announcing mob state changes (crit and death) to the radio.
///     Used for the Survivalist.
/// </summary>
/// <seealso cref="ESMedAlertRadioAnnouncerSystem"/>
[RegisterComponent, Access(typeof(ESMedAlertRadioAnnouncerSystem))]
public sealed partial class ESMedAlertRadioAnnouncerComponent : Component
{
    /// <summary>
    ///     Message to broadcast on entering crit.
    /// </summary>
    [DataField]
    public LocId? CritMessage = "es-mask-survivalist-medalert-crit-message";

    /// <summary>
    ///     Message to broadcast on dying.
    /// </summary>
    [DataField]
    public LocId? DeathMessage = "es-mask-survivalist-medalert-death-message";

    /// <summary>
    ///     Radio channel to broadcast over.
    /// </summary>
    [DataField]
    public ProtoId<RadioChannelPrototype> Channel = "Common";
}
