namespace Content.Shared._ES.Objectives.Target.Components;

/// <summary>
/// Used for a <see cref="ESTargetObjectiveComponent"/> which targets the player with the objective.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTargetSelfObjectiveSystem))]
public sealed partial class ESTargetSelfObjectiveComponent : Component;
