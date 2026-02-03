using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Telesci.Anomaly.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedAnomalySystem))]
public sealed partial class ESPortalAnomalyComponent : Component
{
    [DataField, AutoNetworkedField]
    public int CodeIndex;

    [DataField, AutoNetworkedField]
    public List<ESAnomalySignal> SignalCode = [];

    [DataField]
    public int CodeLength = 4;
}

[Serializable, NetSerializable]
public enum ESAnomalySignal : byte
{
    Alpha,
    Beta,
    Gamma,
    Delta,
    Epsilon,
    Zeta,
}
