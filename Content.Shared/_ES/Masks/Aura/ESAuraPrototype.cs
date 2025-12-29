using Content.Shared.Popups;
using Content.Shared.Whitelist;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Masks.Aura;

/// <summary>
/// An aura used with <see cref="ESSenseAuraSystem"/> to categorize the different auras and when they appear.
/// </summary>
[Prototype("esAura")]
public sealed partial class ESAuraPrototype : IPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    /// <summary>
    /// Popup shown when the aura is detected.
    /// </summary>
    [DataField(required: true)]
    public LocId Description;

    /// <summary>
    /// Popup type to show.
    /// </summary>
    [DataField]
    public PopupType PopupType = PopupType.LargeCaution;

    /// <summary>
    /// Whitelist to determine which objectives satisfy this aura
    /// </summary>
    [DataField]
    public EntityWhitelist? ObjectiveWhitelist;

    /// <summary>
    /// These masks will always be valid for this aura
    /// </summary>
    [DataField]
    public HashSet<ProtoId<ESMaskPrototype>> MaskOverrides = new();

    /// <summary>
    /// If multiple auras are detected, the one with the highest priority will be shown.
    /// </summary>
    [DataField]
    public int Priority = 1;
}
