using Content.Shared.Mobs;
using Robust.Shared.GameStates;

namespace Content.Server._ES.Masks.Objectives.Components;

[RegisterComponent]
[Access(typeof(ESMobStateObjectiveSystem))]
public sealed partial class ESMobStateObjectiveComponent : Component
{
    /// <summary>
    ///     Mob states that count as an objective pass.
    /// </summary>
    [DataField]
    public HashSet<MobState> DesiredStates = [MobState.Alive, MobState.Critical];
}

