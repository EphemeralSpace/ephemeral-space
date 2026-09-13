using Content.Shared._ES.Core.Timer.Components;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Forensics.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESMicroscopeSystem))]
public sealed partial class ESEvidenceMicroscopeComponent : Component
{
    [DataField, AutoNetworkedField]
    public EntityUid? TimerEntity;

    [DataField]
    public SoundSpecifier ScanSound = new SoundPathSpecifier("/Audio/Machines/scanning.ogg");

    [DataField]
    public TimeSpan ScanTime = TimeSpan.FromSeconds(1.5f);

    [DataField]
    public string SlotId = "slide_slot";
}

[Serializable, NetSerializable]
public sealed partial class ESMicroscopeScanTimerEvent : ESEntityTimerEvent;
