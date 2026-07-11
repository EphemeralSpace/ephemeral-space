using Robust.Shared.GameStates;

namespace Content.Shared._ES.Barricade.Components;

/// <summary>
///     Marks an entity which can always have an airlock-style barricade build on top of it.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESBarricadeConstructibleComponent : Component;
