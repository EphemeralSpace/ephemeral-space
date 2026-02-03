namespace Content.Shared._ES.Telesci.Anomaly.Components;

[RegisterComponent]
public sealed partial class ESAnomalyConsoleComponent : Component
{
    [DataField]
    public List<EntityUid> Anomalies = [];
}
