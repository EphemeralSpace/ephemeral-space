using Robust.Shared.GameStates;

namespace Content.Shared._Offbrand.VitalsMonitor;

[RegisterComponent, NetworkedComponent]
[Access(typeof(VitalsMonitorSystem))]
public sealed partial class VitalsMonitorStrapComponent : Component;
