using System.Linq;
using Content.Client._ES.Voting.Ui;
using Content.Client.Gameplay;
using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;

namespace Content.Client._ES.Voting;

[UsedImplicitly]
public sealed class StagehandVoteUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [UISystemDependency] private ESVoteSystem _vote = default!;

    public void OnStateEntered(GameplayState state)
    {
        FindAndUpdateWidget();
    }

    public void OnStateExited(GameplayState state)
    {
    }

    private void OnVoteChanged(Entity<ESVoteComponent> entity, ESVoteOption option, bool selected)
    {
        var netEnt = EntityManager.GetNetEntity(entity);
        EntityManager.RaisePredictiveEvent(new ESSetVoteMessage(netEnt, option, selected));
    }

    public void FindAndUpdateWidget()
    {
        if (UIManager.GetActiveUIWidgetOrNull<ESVotingManagerControl>() is not { } voting)
            return;

        Update(voting);
    }

    public void Update(ESVotingManagerControl voting)
    {
        if (_player.LocalEntity is not { } owner)
            return;

        var votes = _vote.EnumerateVotes().ToList();

        if (votes.Count != voting.LastVotes.Count || votes.Intersect(voting.LastVotes).Count() != votes.Count)
        {
            voting.VotesContainer.Children.Clear();
            foreach (var vote in votes)
            {
                var voteControl = new ESVoteControl
                {
                    Vote = vote,
                };
                voteControl.OnVoteChanged += OnVoteChanged;
                voting.VotesContainer.AddChild(voteControl);
            }
        }

        voting.LastVotes = votes;

        foreach (var child in voting.VotesContainer.Children)
        {
            if (child is ESVoteControl ctrl)
            {
                var comp = EntityManager.GetComponent<ESVoteComponent>(ctrl.Vote);
                ctrl.Update((ctrl.Vote, comp), owner);
            }
        }
    }
}
