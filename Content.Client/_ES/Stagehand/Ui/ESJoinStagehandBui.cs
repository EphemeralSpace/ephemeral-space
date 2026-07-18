using Content.Client._ES.Lobby;
using Content.Shared._ES.Core.Timer;
using Content.Shared._ES.Stagehand;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Stagehand.Ui;

[UsedImplicitly]
public sealed partial class ESJoinStagehandBui : BoundUserInterface
{
    [Dependency] private IUserInterfaceManager _userInterfaceManager = default!;

    private readonly ESDiegeticLobbyUIController _lobbyCurtains;
    private readonly ESEntityTimerSystem _entityTimer;

    private ESStagehandJoinWindow? _window;

    public ESJoinStagehandBui(EntityUid owner, Enum uiKey) : base(owner, uiKey)
    {
        _lobbyCurtains = _userInterfaceManager.GetUIController<ESDiegeticLobbyUIController>();
        _entityTimer = EntMan.System<ESEntityTimerSystem>();
    }

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESStagehandJoinWindow>();

        _window.OnAcceptButtonPressed += () =>
        {
            _lobbyCurtains.StartCurtainAnimation(false);
            _entityTimer.SpawnMethodTimer(TimeSpan.FromSeconds(1.5),
                () =>
                {
                    EntMan.RaisePredictiveEvent(new ESJoinStagehandMessage());
                });
        };
    }
}
