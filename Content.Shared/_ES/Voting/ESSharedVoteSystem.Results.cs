using Content.Shared._ES.Voting.Components;

namespace Content.Shared._ES.Voting;

public abstract partial class ESSharedVoteSystem
{
    private void InitializeResults()
    {
        SubscribeLocalEvent<ESVoteComponent, ESVoteCompletedEvent>(TESTOnVoteCompleted);
    }

    private void TESTOnVoteCompleted(Entity<ESVoteComponent> ent, ref ESVoteCompletedEvent args)
    {
        if (args.Result is not ESEntityPrototypeVoteOption option)
            return;

        PredictedSpawnNextToOrDrop(option.Entity, ent);
    }
}
