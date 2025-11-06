using Robust.Shared.GameStates;

namespace Content.Shared._ES.Degradation.Components;

/// <summary>
/// Marks an entity as about to undergo random degradation.
/// The next time some generic interaction is undergone, the degradation will occur.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESDegradationSystem))]
public sealed partial class ESQueuedDegradationComponent : Component;
