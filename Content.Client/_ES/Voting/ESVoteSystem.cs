using Content.Shared._ES.Voting;
using Content.Shared._ES.Voting.Components;
using Robust.Client.GameObjects;

namespace Content.Client._ES.Voting;

/// <inheritdoc/>
public sealed class ESVoteSystem : ESSharedVoteSystem
{
    [Dependency] private readonly UserInterfaceSystem _userInterface = default!;

    /// <inheritdoc/>
    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<ESVoteComponent, AfterAutoHandleStateEvent>(OnAfterAutoHandleState);
        SubscribeLocalEvent<ESVoteComponent, ComponentShutdown>(OnShutdown);
    }

    private void OnAfterAutoHandleState(Entity<ESVoteComponent> ent, ref AfterAutoHandleStateEvent args)
    {
        var query = EntityQueryEnumerator<ESVoterComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESVoterUiKey.Key, out var bui))
                bui.Update();
        }
    }

    private void OnShutdown(Entity<ESVoteComponent> ent, ref ComponentShutdown args)
    {
        var query = EntityQueryEnumerator<ESVoterComponent, UserInterfaceComponent>();
        while (query.MoveNext(out var uid, out _, out var ui))
        {
            if (_userInterface.TryGetOpenUi((uid, ui), ESVoterUiKey.Key, out var bui))
                bui.Update();
        }
    }
}
