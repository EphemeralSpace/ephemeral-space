using Content.Shared._ES.Telesci.Anomaly.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Telesci.Ui;

[UsedImplicitly]
public sealed class ESAnomalyConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESAnomalyConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESAnomalyConsoleWindow>();
    }

    protected override void UpdateState(BoundUserInterfaceState msg)
    {
        base.UpdateState(msg);

        if (msg is ESAnomalyConsoleBuiState state)
            _window?.Update(state);
    }
}
