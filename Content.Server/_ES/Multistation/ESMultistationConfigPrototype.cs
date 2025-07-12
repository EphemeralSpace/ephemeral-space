using Content.Server.Maps;
using Content.Shared.Dataset;
using Content.Shared.Destructible.Thresholds;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization.TypeSerializers.Implementations.Custom.Prototype.Array;
using Robust.Shared.Utility;

namespace Content.Server._ES.Multistation;

[Prototype("esMultistationConfig")]
public sealed partial class ESMultistationConfigPrototype : IPrototype, IInheritingPrototype
{
    /// <inheritdoc/>
    [IdDataField]
    public string ID { get; } = default!;

    [ParentDataField(typeof(AbstractPrototypeIdArraySerializer<ESMultistationConfigPrototype>))]
    public string[]? Parents { get; private set; }

    [AbstractDataField]
    public bool Abstract { get; private set; }

    [DataField]
    public int PlayersPerStation = 30;

    [DataField]
    public int MinStations = 2;

    [DataField]
    public float StationDistance = 128f;

    [DataField]
    public List<ProtoId<GameMapPrototype>> MapPool = new();
}
