using Robust.Shared.GameStates;

namespace Content.Shared._ES.Destructible.Components;

/// <summary>
/// Component that marks an entity that should be destroyed if it ever gets unanchored.
/// Do not pass go, do not collect 200. Explode.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESDestroyOnUnanchorSystem))]
public sealed partial class ESDestroyOnUnanchorComponent : Component;
