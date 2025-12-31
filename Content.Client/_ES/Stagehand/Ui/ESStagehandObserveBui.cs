using Content.Shared._ES.Stagehand.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Stagehand.Ui;

[UsedImplicitly]
public sealed class ESStagehandObserveBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESStagehandObserveWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindowCenteredRight<ESStagehandObserveWindow>();
        _window.Update();

        _window.OnWarpButtonPressed += mind =>
        {
            EntMan.RaisePredictiveEvent(new ESWarpToMindMessage
            {
                Mind = EntMan.GetNetEntity(mind),
            });
        };
    }

    public override void Update()
    {
        base.Update();

        _window?.Update();
    }
}
