using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Robust.Client.GameObjects;
using Robust.Client.UserInterface;
using Robust.Shared.Timing;

namespace Content.Client._ES.Voting;

/// <inheritdoc/>
public sealed class ESVoteSystem : ESSharedVoteSystem
{
    [Dependency] private readonly IGameTiming _timing = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVoteComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESVoteComponent, ComponentRemove>(OnRemove);
    }

    private void OnAfterAutoHandleState(Entity<ESVoteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        if (!_timing.ApplyingState)
            return;

        _ui.GetUIController<StagehandVoteUIController>().FindAndUpdateWidget();
    }

    private void OnRemove(Entity<ESVoteComponent> ent, ref ComponentRemove args)
    {
        _ui.GetUIController<StagehandVoteUIController>().FindAndUpdateWidget();
    }

    protected override void OnSetVote(ESSetVoteMessage args, EntitySessionEventArgs ev)
    {
        if (_timing.ApplyingState)
            return;

        base.OnSetVote(args, ev);

        if (ev.SenderSession.AttachedEntity is not { } attachedEntity)
            return;

        _ui.GetUIController<StagehandVoteUIController>().FindAndUpdateWidget();
    }
}
