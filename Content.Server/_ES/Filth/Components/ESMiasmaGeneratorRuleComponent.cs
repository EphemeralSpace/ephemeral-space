using Content.Shared._ES.Core.Timer.Components;
using Content.Shared.EntityTable.EntitySelectors;
using Robust.Shared.Map;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._ES.Filth.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESMiasmaGeneratorRuleComponent : Component
{
    public const float MinEventMols = 1.5f;

    [DataField]
    public EntityTableSelector SpawnTable = new NoneSelector();

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(10f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public int TilesPerEvent = 100;
}

public sealed partial class ESMiasmaSpawnTimerEvent : ESEntityTimerEvent
{
    [DataField]
    public EntityCoordinates Coordinates;

    public ESMiasmaSpawnTimerEvent(EntityCoordinates coordinates)
    {
        Coordinates = coordinates;
    }
}
