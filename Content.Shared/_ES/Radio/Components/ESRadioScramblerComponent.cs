using Content.Shared.DoAfter;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Radio.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESRadioScramblerComponent : Component
{
    [DataField, AutoNetworkedField]
    public bool Hacked;

    [DataField]
    public TimeSpan RepairDelay = TimeSpan.FromSeconds(15);
}

[Serializable, NetSerializable]
public enum ESRadioScramblerVisuals : byte
{
    Hacked,
}

[Serializable, NetSerializable]
public sealed partial class ESRepairRadioScramblerDoAfterEvent : SimpleDoAfterEvent;
