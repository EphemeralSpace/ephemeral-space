using Content.Server.Administration;
using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Content.Shared.Administration;
using Robust.Shared.Toolshed;

namespace Content.Server._ES.Voting;

/// <inheritdoc/>
public sealed class ESVoteSystem : ESSharedVoteSystem
{
    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();
    }
}

[ToolshedCommand, AdminCommand(AdminFlags.Fun)]
public sealed class ESVoteCommand : ToolshedCommand
{
    [CommandImplementation("ls")]
    public IEnumerable<Entity<ESVoteComponent>> List()
    {
        var query = EntityManager.EntityQueryEnumerator<ESVoteComponent>();
        while (query.MoveNext(out var uid, out var comp))
        {
            yield return (uid, comp);
        }
    }

    [CommandImplementation("options")]
    public IEnumerable<string> Options([PipedArgument] Entity<ESVoteComponent> vote)
    {
        foreach (var option in vote.Comp.VoteOptions)
        {
            yield return option.DisplayString;
        }
    }

    [CommandImplementation("end")]
    public void End([PipedArgument] Entity<ESVoteComponent> vote)
    {
        var sys = Sys<ESVoteSystem>();
        sys.EndVote(vote);
    }
}
