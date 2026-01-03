using Content.Shared._ES.Telesci.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Telesci.Ui;

[UsedImplicitly]
public sealed class ESPortalGeneratorConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESPortalGeneratorConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESPortalGeneratorConsoleWindow>();

        _window.OnActivatePressed += () =>
        {
            SendMessage(new ESActivePortalGeneratorBuiMessage());
        };
    }

    protected override void UpdateState(BoundUserInterfaceState msg)
    {
        base.UpdateState(msg);

        if (msg is ESPortalGeneratorConsoleBuiState state)
            _window?.Update(state);
    }
}
