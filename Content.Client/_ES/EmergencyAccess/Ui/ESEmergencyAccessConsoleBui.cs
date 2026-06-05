using Content.Shared._ES.EmergencyAccess.Components;
using Robust.Client.UserInterface;

namespace Content.Client._ES.EmergencyAccess.Ui;

public sealed class ESEmergencyAccessConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESEmergencyAccessConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESEmergencyAccessConsoleWindow>();
        _window.Update(Owner, true);

        _window.OnSearchButtonPressed += key =>
        {
            SendMessage(new ESEmergencyAccessSearchBuiMessage(key));
        };

        _window.OnToggleButtonPressed += () =>
        {
            SendMessage(new ESEmergencyAccessToggleBuiMessage());
        };
    }

    public override void Update()
    {
        _window?.Update(Owner);
    }
}
