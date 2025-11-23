using Content.Shared._ES.Stagehand;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Stagehand.Ui;

[UsedImplicitly]
public sealed class ESJoinStagehandBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESStagehandJoinWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESStagehandJoinWindow>();

        _window.OnAcceptButtonPressed += () =>
        {
            EntMan.RaisePredictiveEvent(new ESJoinStagehandMessage());
        };
    }
}
