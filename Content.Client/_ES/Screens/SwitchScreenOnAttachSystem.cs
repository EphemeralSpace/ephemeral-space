using Content.Client.Gameplay;
using Content.Shared._ES.Stagehand.Components;
using Robust.Client.State;
using Robust.Client.UserInterface;
using Robust.Shared.Player;

namespace Content.Client._ES.Screens;

/// <summary>
///     Switches game screen between <see cref="PerformerGameScreen"/> and <see cref="StagehandGameScreen"/> on entity attach.
/// </summary>
public sealed class SwitchScreenOnAttachSystem : EntitySystem
{
    [Dependency] private readonly IStateManager _state = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;

    public override void Initialize()
    {
        base.Initialize();

        SubscribeLocalEvent<LocalPlayerAttachedEvent>(OnAttach);
    }

    private void OnAttach(LocalPlayerAttachedEvent ev)
    {
        if (_state.CurrentState is not GameplayState state)
            return;

        if (_ui.ActiveScreen is PerformerGameScreen && HasComp<ESStagehandComponent>(ev.Entity))
            state.SetScreenType(GameplayStateScreenType.Stagehand);

        if (_ui.ActiveScreen is StagehandGameScreen && !HasComp<ESStagehandComponent>(ev.Entity))
            state.SetScreenType(GameplayStateScreenType.Performer);
    }
}
