using Content.Server.DeviceNetwork.Systems.Devices;

namespace Content.Server.DeviceNetwork.Components.Devices
{
    [RegisterComponent]
    [Access(typeof(ApcNetSwitchSystem))]
    public sealed partial class ApcNetSwitchComponent : Component
    {
// ES START
        [DataField]
// ES END
        [ViewVariables] public bool State;
    }
}
