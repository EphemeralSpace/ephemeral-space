using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;

namespace Content.Server._ES.Degradation.Components;

[RegisterComponent]
[Access(typeof(ESVentSpawnEventSystem))]
public sealed partial class ESVentSpawnEventComponent : Component
{
    /// <summary>
    /// Entities that will be spawned
    /// </summary>
    [DataField]
    public PrototypeTableSelector<EntityPrototype> Table = new NoneSelector<EntityPrototype>();

    [DataField]
    public TimeSpan MinSpawnDelay = TimeSpan.FromSeconds(0);

    [DataField]
    public TimeSpan MaxSpawnDelay = TimeSpan.FromMinutes(2.5f);
}
