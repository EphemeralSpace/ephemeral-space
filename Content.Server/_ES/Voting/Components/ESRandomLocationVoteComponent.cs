namespace Content.Server._ES.Voting.Components;

[RegisterComponent]
[Access(typeof(ESRandomLocationVoteSystem))]
public sealed partial class ESRandomLocationVoteComponent : Component
{
    [DataField]
    public int Count = 4;

    [DataField]
    public bool CheckLOS = true;
}
