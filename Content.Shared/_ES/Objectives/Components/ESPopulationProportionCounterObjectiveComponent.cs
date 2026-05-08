using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Components;

/// <summary>
/// Used in conjunction with <see cref="ESCounterObjectiveComponent"/> to scale <see cref="ESCounterObjectiveComponent.Target"/> in proportion with server population.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedObjectiveSystem))]
public sealed partial class ESPopulationProportionCounterObjectiveComponent : Component
{
    /// <summary>
    /// Proportion of server population that will be used as the target.
    /// </summary>
    [DataField]
    public float Proportion = 0.25f;

    /// <summary>
    /// Lower bound on target
    /// </summary>
    [DataField]
    public float Minimum = 1f;

    /// <summary>
    /// Upper bound on target
    /// </summary>
    [DataField]
    public float Maximum = 10f;
}
