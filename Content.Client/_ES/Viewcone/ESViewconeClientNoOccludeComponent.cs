using Content.Shared._ES.Viewcone;

namespace Content.Client._ES.Viewcone;

/// <summary>
///     Marks an entity which this client should always perceive, even if they have <see cref="ESViewconeOccludableComponent"/>
/// </summary>
/// <remarks>
///     Used for dynamic situations where you should intuitively always show the occludable, like if you're pulling it.
/// </remarks>
[RegisterComponent]
public sealed partial class ESViewconeClientNoOccludeComponent : Component;
