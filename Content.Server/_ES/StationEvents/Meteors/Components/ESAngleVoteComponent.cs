using Content.Shared._ES.Voting.Components;

namespace Content.Server._ES.StationEvents.Meteors.Components;

/// <summary>
/// <see cref="ESVoteComponent"/> for a random angle
/// </summary>
[RegisterComponent]
[Access(typeof(ESMeteorsRule))]
public sealed partial class ESAngleVoteComponent : Component
{
    [DataField]
    public int Count = 4;
}
