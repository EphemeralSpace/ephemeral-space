using Content.Shared._ES.Objectives.Components;
using Content.Shared._ES.Objectives.Kill.Components;

namespace Content.Server._ES.Masks.Avenger.Components;

/// <summary>
/// Version of <see cref="ESKillTargetObjectiveComponent"/> that required the target to be killed via the objective holder rather than just passive death.
/// Uses <see cref="ESCounterObjectiveComponent"/> for state tracking.
/// </summary>
[RegisterComponent]
[Access(typeof(ESDirectKillTargetObjectiveSystem))]
public sealed partial class ESDirectKillTargetObjectiveComponent : Component;
