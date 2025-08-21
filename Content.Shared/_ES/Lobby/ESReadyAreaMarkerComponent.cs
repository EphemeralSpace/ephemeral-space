using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Lobby;

/// <summary>
/// a tile which, when entered, either switches the player to being readied, or switches them to being unreadied
/// done this way to avoid having to care about like.. making an entity to manage an entire trigger volume
/// readyup marker also handles allowing players to latejoin.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
public sealed partial class ESReadyAreaMarkerComponent : Component
{
    [DataField, AutoNetworkedField]
    public ESReadyMarkerBehavior Behavior = ESReadyMarkerBehavior.Ready;
}

[Serializable, NetSerializable]
public enum ESReadyMarkerBehavior
{
    Ready,
    Unready
}
