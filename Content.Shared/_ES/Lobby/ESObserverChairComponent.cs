using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Lobby;

/// <summary>
/// an entity that, when buckled to, will mark a theatergoer as being an observer
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESObserverChairComponent : Component
{
}
