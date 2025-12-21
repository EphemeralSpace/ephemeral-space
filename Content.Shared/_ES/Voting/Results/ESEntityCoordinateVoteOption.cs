using Robust.Shared.Map;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

[Serializable, NetSerializable]
public sealed partial class ESEntityCoordinateVoteOption : ESVoteOption
{
    // Weak Entity Ref Terrorist
    [DataField]
    public NetCoordinates Coordinates;

    public override bool Equals(object? obj)
    {
        return obj is ESEntityCoordinateVoteOption other && Coordinates.Equals(other.Coordinates);
    }

    public override int GetHashCode()
    {
        return Coordinates.GetHashCode();
    }
}
