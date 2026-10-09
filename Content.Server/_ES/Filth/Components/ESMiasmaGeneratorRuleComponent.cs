using Content.Shared.PrototypeTable.PrototypeSelectors;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom;

namespace Content.Server._ES.Filth.Components;

[RegisterComponent, AutoGenerateComponentPause]
public sealed partial class ESMiasmaGeneratorRuleComponent : Component
{
    public const float MinEventMols = 1.5f;

    [DataField]
    public PrototypeTableSelector<EntityPrototype> SpawnTable = new NoneSelector<EntityPrototype>();

    [DataField]
    public TimeSpan UpdateRate = TimeSpan.FromSeconds(10f);

    [DataField(customTypeSerializer: typeof(TimeOffsetSerializer)), AutoPausedField]
    public TimeSpan NextUpdate;

    [DataField]
    public int TilesPerEvent = 100;
}
