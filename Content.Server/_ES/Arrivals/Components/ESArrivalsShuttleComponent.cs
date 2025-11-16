namespace Content.Server._ES.Arrivals.Components;

[RegisterComponent, Access(typeof(ESArrivalsSystem))]
public sealed partial class ESArrivalsShuttleComponent : Component
{
    [DataField]
    public EntityUid Station;
}
