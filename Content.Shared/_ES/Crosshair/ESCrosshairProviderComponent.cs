using Robust.Shared.GameStates;

namespace Content.Shared._ES.Crosshair;

/// <summary>
///     When held in hand, allows an entity with <see cref="ESCrosshairAimerComponent"/> to spawn a crosshair
///     while in combat mode, or without if <see cref="RequiresCombatMode"/> is false.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESCrosshairProviderComponent : Component
{
    [DataField]
    public bool RequiresCombatMode = true;
}
