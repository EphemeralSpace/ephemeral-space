using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

[Serializable, NetSerializable]
public sealed partial class ESAngleVoteOption : ESVoteOption
{
    [DataField]
    public Angle Angle;

    public override bool Equals(object? obj)
    {
        return obj is ESAngleVoteOption other && Angle.Equals(other.Angle);
    }

    public override int GetHashCode()
    {
        return Angle.GetHashCode();
    }
}
