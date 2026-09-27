using Robust.Shared.GameStates;

namespace Content.Shared._ES.Objectives.Components;

/// <summary>
/// Marker component that indicates an objective should not be shown on the round summary.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESHideSummaryObjectiveComponent : Component;
