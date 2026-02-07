using Content.Client.Gameplay;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Systems.Character;
using Content.Client.UserInterface.Systems.MenuBar.Widgets;
using Content.Shared._ES.CCVar;
using Content.Shared.Input;
using JetBrains.Annotations;
using Robust.Client.Player;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Input.Binding;

namespace Content.Client._ES.Mind.Ui;

[UsedImplicitly]
public sealed class ESCharacterUIController : UIController, IOnStateEntered<GameplayState>, IOnStateExited<GameplayState>
{
    [Dependency] private readonly IPlayerManager _player = default!;
    [Dependency] private readonly IConfigurationManager _cfg = default!;

    private ESCharacterWindow? _window;

    private MenuButton? CharacterButton => UIManager.GetActiveUIWidgetOrNull<GameTopMenuBar>()?.CharacterButton;

    public void OnStateEntered(GameplayState state)
    {
        _window = UIManager.CreateWindow<ESCharacterWindow>();
        LayoutContainer.SetAnchorPreset(_window, LayoutContainer.LayoutPreset.CenterTop);

        _window.OnClose += DeactivateButton;
        _window.OnOpen += ActivateButton;

        _player.LocalPlayerAttached += OnLocalPlayerAttached;

        CommandBinds.Builder
            .Bind(ContentKeyFunctions.OpenCharacterMenu,
                InputCmdHandler.FromDelegate(_ => ToggleWindow()))
            .Register<CharacterUIController>();

        if (_cfg.GetCVar(ESCVars.ESOpenCharacterMenuOnSpawn) && !_window.IsOpen)
            _window?.OpenCenteredLeft();
    }

    public void OnStateExited(GameplayState state)
    {
        if (_window != null)
        {
            _window.Close();
            _window = null;
        }

        _player.LocalPlayerAttached -= OnLocalPlayerAttached;

        CommandBinds.Unregister<CharacterUIController>();
    }

    private void DeactivateButton()
    {
        CharacterButton?.SetClickPressed(false);
    }

    private void ActivateButton()
    {
        CharacterButton?.SetClickPressed(true);
    }

    private void CharacterButtonPressed(BaseButton.ButtonEventArgs obj)
    {
        ToggleWindow();
    }

    private void OnLocalPlayerAttached(EntityUid obj)
    {
        _window?.Update();
    }

    private void ToggleWindow()
    {
        if (_window == null)
            return;

        CharacterButton?.SetClickPressed(!_window.IsOpen);

        if (_window.IsOpen)
        {
            _window.Close();
        }
        else
        {
            _window.Update();
            _window.Open();
        }
    }

    public void UnloadButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.OnPressed -= CharacterButtonPressed;
    }

    public void LoadButton()
    {
        if (CharacterButton == null)
            return;

        CharacterButton.OnPressed += CharacterButtonPressed;
    }
}
