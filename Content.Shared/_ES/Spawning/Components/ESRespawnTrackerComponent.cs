using Robust.Shared.GameStates;
using Robust.Shared.Network;

namespace Content.Shared._ES.Spawning.Components;

/// <summary>
/// Holds respawn data for serialization purposes.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSharedSpawningSystem))]
public sealed partial class ESRespawnTrackerComponent : Component
{
    /// <summary>
    /// Dictionary denoting when a given player is able to respawn.
    /// </summary>
    [DataField, AutoNetworkedField]
    public Dictionary<NetUserId, TimeSpan> RespawnTimes = new();
}
