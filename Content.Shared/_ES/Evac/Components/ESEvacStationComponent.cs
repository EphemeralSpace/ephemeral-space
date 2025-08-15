using Robust.Shared.GameStates;

namespace Content.Shared._ES.Evac.Components;

/// <summary>
/// Tracks a beacon used for coordinating an evac shuttle call.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState(true), AutoGenerateComponentPause]
[Access(typeof(ESSharedEvacSystem))]
public sealed partial class ESEvacStationComponent : Component
{
    /// <summary>
    /// Whether the beacon is currently active
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool EvacVoteEnabled;

    /// <summary>
    /// The time at which the beacon can be toggled again.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan NextToggleTime;

    /// <summary>
    /// Delay applied to evac votes at the beginning of a round.
    /// </summary>
    [DataField]
    public TimeSpan RoundstartDelay = TimeSpan.FromMinutes(20.0f);

    /// <summary>
    /// The min delay for when the beacon can be toggled.
    /// </summary>
    [DataField]
    public TimeSpan ToggleDelay = TimeSpan.FromMinutes(2.5f);

    /// <summary>
    /// Set on round-end, when all the beacons are locked and you can no longer recall.
    /// </summary>
    [DataField, AutoNetworkedField]
    public bool Locked;

    /// <summary>
    /// Hack since round-end isn't networked.
    /// </summary>
    [DataField, AutoNetworkedField, AutoPausedField]
    public TimeSpan? RoundEndTime;
}
