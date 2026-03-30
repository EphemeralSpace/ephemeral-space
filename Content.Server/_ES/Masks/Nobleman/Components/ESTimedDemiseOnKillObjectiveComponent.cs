using Content.Shared._ES.Core.Timer.Components;

namespace Content.Server._ES.Masks.Nobleman.Components;

[RegisterComponent]
[Access(typeof(ESTimedDemiseOnKillObjectiveSystem))]
public sealed partial class ESTimedDemiseOnKillObjectiveComponent : Component
{
    [DataField]
    public float DefaultProgress = 1f;

    /// <summary>
    /// Time it takes to die after killing someone.
    /// </summary>
    [DataField]
    public TimeSpan KillDelay = TimeSpan.FromMinutes(5);

    [DataField]
    public LocId NotificationTitle;

    [DataField]
    public LocId NotificationBody;

    [DataField]
    public bool KilledAnyone;
}

public sealed partial class ESTimedDemiseOnKillEvent : ESEntityTimerEvent;
