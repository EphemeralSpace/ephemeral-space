using Robust.Shared.GameStates;

namespace Content.Shared._ES.Masks.Phantom.Components;

/// <summary>
/// Marker component for when the phantom materializes and becomes tangible.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESPhantomSystem))]
public sealed partial class ESPhantomMaterializedComponent : Component;
