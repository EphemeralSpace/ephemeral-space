using Content.Shared._ES.Objectives.Components;

namespace Content.Server._ES.Masks.Vigilante.Components;

/// <summary>
/// Objective that works with <see cref="ESCounterObjectiveComponent"/> to add progress each time a "killer" is killed.
/// </summary>
[RegisterComponent]
[Access(typeof(ESKillKillerObjectiveSystem))]
public sealed partial class ESKillKillerObjectiveComponent : Component;
