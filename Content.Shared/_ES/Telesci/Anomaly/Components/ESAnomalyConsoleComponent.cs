using Robust.Shared.Serialization;

namespace Content.Shared._ES.Telesci.Anomaly.Components;

[RegisterComponent]
public sealed partial class ESAnomalyConsoleComponent : Component
{
    [DataField]
    public List<EntityUid> Anomalies = [];
}

[Serializable, NetSerializable]
public sealed class ESAnomalyConsoleBuiState : BoundUserInterfaceState
{
    public List<(string name, List<ESAnomalySignal> signals, int signalCount)> Anomalies = [];
}

[Serializable, NetSerializable]
public enum ESAnomalyConsoleUiKey : byte
{
    Key,
}
