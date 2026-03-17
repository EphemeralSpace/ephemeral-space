using Robust.Shared.GameStates;

namespace Content.Shared._ES.Degradation.Components;

/// <summary>
/// Marker for players which causes them to cause airlock degradation when opening airlocks.
/// </summary>
[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESAirlockFailureSystem))]
public sealed partial class ESAirlockFailerComponent : Component
{
    /// <summary>
    /// Amount of times this will trigger
    /// </summary>
    [DataField, AutoNetworkedField]
    public int Charges;

    /// <summary>
    /// Chance per door opening to fail
    /// </summary>
    [DataField]
    public float FailChance = 0.33f;
}
