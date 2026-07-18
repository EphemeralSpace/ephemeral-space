using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._ES.StationEvents.VentSwarm.Components;

[RegisterComponent, AutoGenerateComponentPause]
[Access(typeof(ESVentSwarmRule))]
public sealed partial class ESVentSwarmRuleComponent : Component
{
    [DataField]
    public EntityUid? Vent;

    [DataField]
    public EntityTableSelector SpawnTable;

    [DataField]
    public int MinSwarmCount = 6;

    [DataField]
    public int MaxSwarmCount = 12;

    [DataField]
    public int SwarmCount;

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextSwarmTime;

    [DataField]
    public TimeSpan MinSwarmDelay = TimeSpan.FromSeconds(1);

    [DataField]
    public TimeSpan MaxSwarmDelay = TimeSpan.FromSeconds(3);
}
