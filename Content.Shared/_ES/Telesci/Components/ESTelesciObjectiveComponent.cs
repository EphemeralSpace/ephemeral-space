using Robust.Shared.GameStates;

namespace Content.Shared._ES.Telesci.Components;

/// <summary>
/// Basic objective that progresses based on the <see cref="ESTelesciStationComponent"/> stages.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedTelesciSystem))]
public sealed partial class ESTelesciObjectiveComponent : Component;
