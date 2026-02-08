using Content.Shared._Offbrand.Wounds;
using Robust.Shared.Audio;
using Robust.Shared.GameStates;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;
using Robust.Shared.Serialization;

namespace Content.Shared._Offbrand.VitalsMonitor;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState, AutoGenerateComponentPause]
[Access(typeof(VitalsMonitorSystem))]
public sealed partial class VitalsMonitorComponent : Component
{
    // Scan data
    [DataField, AutoNetworkedField]
    public EntityUid? Scanning;

    [DataField, AutoNetworkedField]
    public WoundableHealthAnalyzerData? ScanData;

    [DataField]
    public float? MaxScanRange = 2.5f;

    // Audio stuff
    [DataField, AutoNetworkedField]
    public EntityUid? LoopingAudio;

    [DataField, AutoNetworkedField]
    public SoundSpecifier? CurrentAudio;

    [DataField]
    public SortedDictionary<float, SoundSpecifier> PulseAudioThresholds;

    [DataField]
    public SoundSpecifier AsystoleAudio;

    // Update data
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    // Thresholds for sprites
    [DataField]
    public SortedDictionary<float, VitalsMonitorBrainActivity> BrainActivityThresholds;

    [DataField]
    public SortedDictionary<float, bool> BrainActivityWarningThresholds;

    [DataField]
    public SortedDictionary<float, VitalsMonitorBreathing> BreathingThresholds;

    [DataField]
    public SortedDictionary<float, bool> BreathingWarningThresholds;

    [DataField]
    public SortedDictionary<float, VitalsMonitorPulse> PulseThresholds;

    [DataField]
    public SortedDictionary<float, bool> PulseWarningThresholds;

    // Messages
    [DataField]
    public LocId ScanningPatient = "vitals-monitor-scanning-patient";

    [DataField]
    public LocId ScanningStrap = "vitals-monitor-scanning-strap";
}

[Serializable, NetSerializable]
public enum VitalsMonitorVisuals : byte
{
    BrainActivity,
    BrainActivityWarning,
    Breathing,
    BreathingWarning,
    Pulse,
    PulseWarning,
}

[Serializable, NetSerializable]
public enum VitalsMonitorBrainActivity : byte
{
    Blank,
    Okay,
    Bad,
    VeryBad,
}

[Serializable, NetSerializable]
public enum VitalsMonitorBreathing : byte
{
    Blank,
    Normal,
    Shallow,
}

[Serializable, NetSerializable]
public enum VitalsMonitorPulse : byte
{
    Blank,
    Asystole,
    Normal,
    Fast,
    VentricularTachycardia,
}
