using Robust.Shared.GameStates;

namespace Content.Shared._ES.Masks.Traitor.Components;

/// <summary>
/// Denotes an object that can be sabotaged by members of a particular troupe.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSabotageSystem))]
public sealed partial class ESSabotageTargetComponent : Component
{
    /// <summary>
    /// Amount of time it takes for the sabotage doafter to complete.
    /// </summary>
    [DataField]
    public TimeSpan SabotageTime = TimeSpan.FromSeconds(10);
}
