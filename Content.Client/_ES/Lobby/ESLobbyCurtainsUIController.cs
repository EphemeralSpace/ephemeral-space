using System.Numerics;
using Content.Client.Lobby;
using Content.Client.Resources;
using Content.Shared.CCVar;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Client._ES.Lobby;

public sealed class ESLobbyCurtainsUIController : UIController
{
    [Dependency] private readonly IConfigurationManager _cfg = default!;
    [Dependency] private readonly IUserInterfaceManager _ui = default!;
    [Dependency] private readonly IConsoleHost _conHost = default!;
    [Dependency] private readonly IResourceCache _resCache = default!;

    private bool _showAnimation = true;
    private LobbyCurtainState _curtainState = LobbyCurtainState.Open;

    private LayoutContainer _curtainRoot = default!;
    private TextureRect _leftCurtain = default!;
    private TextureRect _rightCurtain = default!;
    private TextureRect _curtainBar = default!;

    public override void Initialize()
    {
        base.Initialize();

        _cfg.OnValueChanged(CCVars.GameLobbyCurtainAnimation, b => _showAnimation = b, true);
        _conHost.RegisterCommand("togglelobbycurtains", "Toggles the lobby curtains animation", "togglelobbycurtains", (_, _, _) => StartCurtainAnimation(_curtainState < LobbyCurtainState.Opening));

        CreateCurtainControls();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (_curtainState is not (LobbyCurtainState.Closing or LobbyCurtainState.Opening))
            return;

        var sign = _curtainState is LobbyCurtainState.Opening ? -1 : 1;
        // TODO uhh this should be based on width not
        // absolute px
        // actually interpolate it girl
        var absoluteAmountToMove = args.DeltaSeconds * 500; // px per sec

        // dude this shit is frying my brain im so tired.
        LayoutContainer.SetPosition(_leftCurtain, new Vector2(_leftCurtain.Position.X + absoluteAmountToMove * sign, 0));
        LayoutContainer.SetPosition(_rightCurtain, new Vector2(_rightCurtain.Position.X + -absoluteAmountToMove * sign, 0));

        // this control flow doesn't make any sense i think lol whatever
        if ((_curtainState is LobbyCurtainState.Closing && _leftCurtain.Position.X >= -10) // small leeway
            || (_curtainState is LobbyCurtainState.Opening && _leftCurtain.Position.X < -_leftCurtain.SetWidth - 10))
        {
            _curtainState = _curtainState switch
            {
                LobbyCurtainState.Closing => LobbyCurtainState.Closed,
                LobbyCurtainState.Opening => LobbyCurtainState.Open,
                _ => _curtainState,
            };

            if (_curtainState == LobbyCurtainState.Open)
            {
                _leftCurtain.Visible = false;
                _rightCurtain.Visible = false;
            }
        }
    }

    /// <summary>
    ///     Creates the controls for the curtain animation and attaches them to the UI root
    /// </summary>
    private void CreateCurtainControls()
    {
        _curtainRoot = new LayoutContainer { Name = "LobbyCurtainRoot" };
        _ui.RootControl.AddChild(_curtainRoot);

        _leftCurtain = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Scale,
            Texture =
                _resCache.GetTexture("/Textures/_ES/Interface/Lobby/curtains.png"),
            // TODO MIRROR LOBBY uhhh
            Visible = false,
        };
        _rightCurtain = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Scale,
            Texture =
                _resCache.GetTexture("/Textures/_ES/Interface/Lobby/curtains.png"),
            // TODO MIRROR LOBBY uhhh
            Visible = false,
        };
        _curtainRoot.AddChild(_leftCurtain);
        _curtainRoot.AddChild(_rightCurtain);
        // LayoutContainer.SetAnchorPreset(_leftCurtain, LayoutContainer.LayoutPreset.Wide);
        // LayoutContainer.SetAnchorPreset(_rightCurtain, LayoutContainer.LayoutPreset.TopRight);
    }

    private void StartCurtainAnimation(bool toOpen)
    {
        if (!_showAnimation)
            return;

        var old = _curtainState;

        if ((toOpen && _curtainState > LobbyCurtainState.Closing) ||
            (!toOpen && _curtainState < LobbyCurtainState.Opening))
            return;

        _curtainState = toOpen ? LobbyCurtainState.Opening : LobbyCurtainState.Closing;
        Logger.Info($"Current status {old} next status {_curtainState}");
        var extraWidth = 100;

        _leftCurtain.SetWidth = (_curtainRoot.Width / 2) + extraWidth; // slightly larger than half the window?
        _leftCurtain.Visible = true;

        _rightCurtain.SetWidth = (_curtainRoot.Width / 2) + extraWidth;
        _rightCurtain.Visible = true;

        if (toOpen)
        {
            LayoutContainer.SetPosition(_leftCurtain, Vector2.Zero);
            LayoutContainer.SetPosition(_rightCurtain, new Vector2(_rightCurtain.SetWidth - 2 * extraWidth, 0));
        }
        else
        {
            LayoutContainer.SetPosition(_leftCurtain, new Vector2(-_leftCurtain.SetWidth, 0));
            LayoutContainer.SetPosition(_rightCurtain, new Vector2(_curtainRoot.Width, 0));
        }
    }
}

public enum LobbyCurtainState : byte
{
    Closed = 0,
    Closing = 1,
    Opening = 2,
    Open = 3,
}
