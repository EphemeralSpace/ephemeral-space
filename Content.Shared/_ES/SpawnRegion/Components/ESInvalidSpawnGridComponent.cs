using Robust.Shared.GameStates;

namespace Content.Shared._ES.SpawnRegion.Components;

/// <summary>
///     Marks a grid as invalid for spawning anything on using <see cref="ESSharedSpawnRegionSystem"/>,
///     even if it's technically part of a station.
/// </summary>
/// <remarks>
///     Used for shuttles and whatnot.
/// </remarks>
[RegisterComponent, NetworkedComponent]
public sealed partial class ESInvalidSpawnGridComponent : Component;
