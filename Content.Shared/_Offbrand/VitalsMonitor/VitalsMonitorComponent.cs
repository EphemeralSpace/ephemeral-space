using Content.Shared._Offbrand.Wounds;
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

    // Update data
    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoNetworkedField, AutoPausedField]
    public TimeSpan NextUpdate = TimeSpan.Zero;

    [DataField]
    public TimeSpan UpdateInterval = TimeSpan.FromSeconds(1);

    // Thresholds for sprites
    [DataField(required: true)]
    public SortedDictionary<float, VitalsMonitorBrainActivity> BrainActivityThresholds;

    [DataField(required: true)]
    public SortedDictionary<float, bool> BrainActivityWarningThresholds;

    [DataField(required: true)]
    public SortedDictionary<float, VitalsMonitorBreathing> BreathingThresholds;

    [DataField(required: true)]
    public SortedDictionary<float, bool> BreathingWarningThresholds;

    [DataField(required: true)]
    public SortedDictionary<float, VitalsMonitorPulse> PulseThresholds;

    [DataField(required: true)]
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
