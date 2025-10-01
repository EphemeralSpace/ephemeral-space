using Robust.Shared.Prototypes;

namespace Content.Server._ES.Station.Components;

[RegisterComponent]
public sealed partial class ESStationMapComponent : Component
{
    [DataField]
    public ProtoId<ESStationConfigPrototype> Config;

    [DataField]
    public bool GridsLoaded;
}
