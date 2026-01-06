using Robust.Shared.GameStates;
using Robust.Shared.Prototypes;

namespace Content.Shared._ES.Chemistry.Components;

/// <summary>
/// This is used to change an entity's name and description to another entity when a given solution is empty.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESDisguiseOnSolutionEmptySystem))]
public sealed partial class ESDisguiseOnSolutionEmptyComponent : Component
{
    /// <summary>
    /// Solution to check for empty.
    /// </summary>
    [DataField]
    public string Solution;

    /// <summary>
    /// The entity that will be used for the "disguised" name and description
    /// </summary>
    [DataField]
    public EntProtoId Disguise;
}
