using Content.Server.Maps;
using Content.Shared.Dataset;
using Content.Shared.EntityTable.ValueSelector;
using Content.Shared.Procedural;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;

namespace Content.Server._ES.Station;

[Prototype("esStationConfig")]
public sealed partial class ESStationConfigPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; private set; }  = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESStationConfigPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField]
    public int PlayersPerStation = 30;

    [DataField]
    public int MinStations = 2;

    [DataField]
    public int MaxStations = int.MaxValue;

    [DataField]
    public float StationDistance = 128f;

    /// <summary>
    /// Components applied to the map.
    /// </summary>
    [DataField]
    public ComponentRegistry MapComponents = new();

    /// <summary>
    /// Components applied to all station grids.
    /// </summary>
    [DataField]
    public ComponentRegistry StationGridComponents = new();

    /// <summary>
    /// procgen grids to spawn
    /// </summary>
    [DataField]
    public List<ESStationDungeonDef> Dungeons = new();
}

/// <summary>
/// Contains data for spawning in a procgen grid
/// </summary>
[DataDefinition]
public partial struct ESStationDungeonDef()
{
    /// <summary>
    /// The number of this grid
    /// </summary>
    [DataField]
    public NumberSelector Count = new ConstantNumberSelector(1);

    /// <summary>
    /// How far from the center they will spawn
    /// </summary>
    [DataField]
    public NumberSelector Distance = new ConstantNumberSelector(128);

    /// <summary>
    /// List of configs that will be chosen from.
    /// </summary>
    [DataField]
    public List<ProtoId<DungeonConfigPrototype>> Configs = new();

    /// <summary>
    /// Components added to each grid.
    /// </summary>
    [DataField]
    public ComponentRegistry Components = new();

    [DataField]
    public bool ForcePos = false;

    [DataField]
    public ProtoId<LocalizedDatasetPrototype>? Name = null;
}
