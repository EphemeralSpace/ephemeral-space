namespace Content.Server._ES.Arrivals.Components;

/// <summary>
/// Denotes someone riding on the arrivals shuttle.
/// </summary>
[RegisterComponent]
public sealed partial class ESArrivalsPassengerComponent : Component
{
    [DataField]
    public EntityUid Station;
}
