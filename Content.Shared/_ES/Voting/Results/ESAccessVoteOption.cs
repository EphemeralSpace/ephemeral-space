using Content.Shared.Access;
using Robust.Shared.Prototypes;
using Robust.Shared.Serialization;

namespace Content.Shared._ES.Voting.Results;

[Serializable, NetSerializable]
public sealed partial class ESAccessVoteOption : ESVoteOption
{
    [DataField]
    public ProtoId<AccessGroupPrototype> Access;

    public override bool Equals(object? obj)
    {
        return obj is ESAccessVoteOption other && Access.Equals(other.Access);
    }

    public override int GetHashCode()
    {
        return Access.GetHashCode();
    }

    public ESAccessVoteOption(AccessGroupPrototype access)
    {
        Access = access;
        DisplayString = access.GetAccessGroupName();
    }
}
