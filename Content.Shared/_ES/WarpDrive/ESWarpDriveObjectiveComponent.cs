using Robust.Shared.GameStates;

namespace Content.Shared._ES.WarpDrive;

/// <summary>
/// Basic objective that progresses based on whether the warp drive has charged or not.
/// </summary>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESWarpDriveObjectiveComponent : Component;
