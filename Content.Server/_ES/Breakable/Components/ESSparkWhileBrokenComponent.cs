using Content.Shared._ES.Sparks.Components;

namespace Content.Server._ES.Breakable.Components;

[RegisterComponent]
[Access(typeof(ESSparkWhileBrokenSystem))]
public sealed partial class ESSparkWhileBrokenComponent : ESBaseSparkConfigurationComponent
{
    [DataField]
    public float SparkChancePerSecond = 0.016f;

    [DataField]
    public bool Enabled;
}
