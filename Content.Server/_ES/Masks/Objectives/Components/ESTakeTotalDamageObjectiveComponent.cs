using Content.Shared._ES.Objectives.Components;

namespace Content.Server._ES.Masks.Objectives.Components;

/// <summary>
/// Objective used with <see cref="ESCounterObjectiveComponent"/> for a cumulative amount of lifetime damage.
/// </summary>
[RegisterComponent]
[Access(typeof(ESTakeTotalDamageObjectiveSystem))]
public sealed partial class ESTakeTotalDamageObjectiveComponent : Component;
