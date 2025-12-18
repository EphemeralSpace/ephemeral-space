using Robust.Shared.GameStates;

namespace Content.Server._ES.Masks.Objectives.Components;

[RegisterComponent, NetworkedComponent, AutoGenerateComponentState]
[Access(typeof(ESSurviveObjectiveSystem))]
public sealed partial class ESSurviveObjectiveComponent : Component;
