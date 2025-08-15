using Content.Shared._ES.Evac.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Evac.Ui;

[UsedImplicitly]
public sealed class ESEvacBeaconBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESEvacConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESEvacConsoleWindow>();
        _window.UpdateStationStatuses(Owner);
        _window.OpenCentered();

        _window.OnToggleButtonPressed += () =>
        {
            SendMessage(new ESToggleStationEvacMessage());
        };
    }

    public override void Update()
    {
        base.Update();

        _window?.UpdateStationStatuses(Owner);
    }
}
