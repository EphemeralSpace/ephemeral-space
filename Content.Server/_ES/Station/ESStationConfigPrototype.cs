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
}
