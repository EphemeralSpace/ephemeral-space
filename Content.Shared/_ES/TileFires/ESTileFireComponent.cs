using Robust.Shared.GameStates;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.TileFires;

/// <summary>
///     Handles growth behavior for tile fires, as well as things like requiring oxygen.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESTileFireComponent : Component
{
}
