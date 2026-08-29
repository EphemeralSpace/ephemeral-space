using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Radio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESRadioScramblerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hacked;

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(15);

    /// <summary>
    /// How long after sabotaging this machine until you can repair it again.
    /// </summary>
    [DataField]
    public TimeSpan RepairBlockDelay = TimeSpan.FromMinutes(5);

    [DataField]
    public EntProtoId RepairBlockStatusEffect = "ESStatusEffectRadioScramblerRepairBlocked";
}

[RegisterComponent, NetworkedComponent]
public sealed partial class ESRadioScramblerRepairBlockedStatusEffectComponent : Component;

[Serializable, NetSerializable]
public enum ESRadioScramblerVisuals : byte
{
    Hacked,
}

[Serializable, NetSerializable]
public sealed partial class ESRepairRadioScramblerDoAfterEvent : SimpleDoAfterEvent;
