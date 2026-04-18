using Robust.Shared.GameStates;

namespace Content.Shared._ES.Telesci.Components;

/// <summary>
/// Basic objective that progresses based on whether the warp drive has charged or not.
/// </summary>
[RegisterComponent, NetworkedComponent]
[Access(typeof(ESSharedTelesciSystem))]
public sealed partial class ESWarpDriveObjectiveComponent : Component;
