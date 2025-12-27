using Robust.Shared.GameStates;

namespace Content.Shared._ES.Masks.Traitor.Components;

/// <summary>
///     Marks an entity which is always capable of sabotaging a <see cref="ESSabotageTargetComponent"/>.
///     Can be on the entity itself, or their mind.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESCanAlwaysSabotageComponent : Component;
