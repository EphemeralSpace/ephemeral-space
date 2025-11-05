using Content.Shared._ES.Nuke.Components;
using JetBrains.Annotations;
using Robust.Client.UserInterface;

namespace Content.Client._ES.Nuke.Ui;

[UsedImplicitly]
public sealed class ESCryptoNukeConsoleBui(EntityUid owner, Enum uiKey) : BoundUserInterface(owner, uiKey)
{
    private ESCryptoNukeConsoleWindow? _window;

    protected override void Open()
    {
        base.Open();

        _window = this.CreateWindow<ESCryptoNukeConsoleWindow>();
        _window.Update(Owner);

        _window.OnHackButtonPressed += () => SendPredictedMessage(new ESHackCryptoNukeConsoleBuiMessage());
    }

    protected override void UpdateState(BoundUserInterfaceState state)
    {
        base.UpdateState(state);

        if (state is ESCryptoNukeConsoleBuiState esState)
            _window?.Update(Owner, esState);
    }
}
