using Robust.Shared.Serialization;

namespace Content.Shared._ES.Core.Entity;

[Serializable, NetSerializable]
public enum ESSpawnStrategy : byte
{
    // Randomly centered around a coordinate
    Random,
    // Spawned in a horizontal line through a coordinate
    Horizontal,
    // Spawned in a vertical line through a coordinate
    Vertical,
    // Spawned in a diagonal line going from bottom left to top right through the coordinate
    Diagonal,
}
