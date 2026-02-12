using Content.Shared.DoAfter;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Telesci.Anomaly.Components;

[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedAnomalySystem))]
public sealed partial class ESAnomalyProbeComponent : Component
{
    [DataField]
    public TimeSpan ProbeTime = TimeSpan.FromSeconds(5);

    [DataField]
    public SoundSpecifier? CompleteSound = new SoundPathSpecifier("/Audio/Machines/sonar-ping.ogg")
    {
        Params = new AudioParams { Volume = -3 },
    };
}

[Serializable, NetSerializable]
public sealed partial class ESProbeAnomalyDoAfterEvent : DoAfterEvent
{
    public override DoAfterEvent Clone() => this;
}
