using Content.Server.StationEvents.Events;

namespace Content.Server._ES.Masks.Nobleman;

[RegisterComponent]
public sealed partial class ESTimedDemiseOnKillObjectiveComponent : Component
{
    [DataField]
    public float DefaultProgress = 1f;

    /// <summary>
    ///     time before the FILITHY elite die for their sins
    /// </summary>
    [DataField]
    public TimeSpan TimeBeforeNoblemanDeath = TimeSpan.FromMinutes(5);

    [DataField]
    public bool KilledAnyone = false;
}
