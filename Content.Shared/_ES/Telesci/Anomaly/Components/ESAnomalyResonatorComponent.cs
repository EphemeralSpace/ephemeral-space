using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Telesci.Anomaly.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedAnomalySystem))]
public sealed partial class ESAnomalyResonatorComponent : Component
{
    [DataField, AutoNetworkedField]
    public ESAnomalySignal CurrentSignal = ESAnomalySignal.Zeta;
}

[Serializable, NetSerializable]
public enum ESAnomalyResonatorVisuals : byte
{
    Mode,
}
