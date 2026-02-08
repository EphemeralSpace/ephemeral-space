using Robust.Shared.Utility;

namespace Content.Server._ES.Arrivals.Components;

[RegisterComponent, Access(typeof(ESArrivalsSystem)), AutoGenerateComponentPause]
public sealed partial class ESStationArrivalsComponent : Component
{
    [DataField]
    public ResPath ShuttlePath = new("/Maps/_ES/Shuttles/arrivals.yml");

    [DataField]
    public EntityUid? ShuttleUid;

    [DataField, AutoPausedField]
    public TimeSpan ArrivalTime;
}
