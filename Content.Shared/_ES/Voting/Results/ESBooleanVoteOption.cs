using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

[Serializable, NetSerializable]
public sealed partial class ESBooleanVoteOption : ESVoteOption
{
    [DataField]
    public bool Value;

    public override bool Equals(object? obj)
    {
        return obj is ESBooleanVoteOption other && Value.Equals(other.Value);
    }

    public override int GetHashCode()
    {
        return Value.GetHashCode();
    }
}
