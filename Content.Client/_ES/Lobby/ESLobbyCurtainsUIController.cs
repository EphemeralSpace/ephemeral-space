using System.Numerics;
using Content.Client.Lobby;
using Content.Client.Resources;
using Content.Shared.CCVar;
using JetBrains.Annotations;
using Robust.Client.ResourceManagement;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controllers;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;
using Robust.Shared.Console;
using Robust.Shared.Timing;

namespace Content.Client._ES.Lobby;

/// <summary>
///     Handles the opening/closing curtains animation when lobby->game or gameend->lobby transitions
///     Creates controls on init and attaches them to the root control, sorry
/// </summary>
[UsedImplicitly]
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

    private const int ExtraWidth = 100;

    private static readonly TimeSpan DefaultAnimationTime = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan ClosedPanicOpenTime = TimeSpan.FromSeconds(10);
    private float _currentTargetTime = 0f;
    private float _accumulatedTime = 0f;
    private float _timeSpentClosed = 0f; // measured so we can panic-open the curtains if theyre closed for too long for some reason
    private float _leftStartingX = 0f;
    private float _rightStartingX = 0f;

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

        if (_curtainState is LobbyCurtainState.Closed)
        {
            _timeSpentClosed += args.DeltaSeconds;
            if (_timeSpentClosed > ClosedPanicOpenTime.TotalSeconds)
            {
                Log.Warning("");
                StartCurtainAnimation(true, TimeSpan.FromSeconds(0.5));
                _timeSpentClosed = 0f;
                return;
            }
        }

        if (_curtainState is not (LobbyCurtainState.Closing or LobbyCurtainState.Opening))
            return;

        _accumulatedTime += args.DeltaSeconds;

        var t = Math.Clamp(_accumulatedTime / _currentTargetTime, 0f, 1f);

        var leftTargetXPos = _curtainState is LobbyCurtainState.Opening
            ? -_leftCurtain.SetWidth
            : 0;
        var rightTargetXPos = _curtainState is LobbyCurtainState.Opening
            ? _curtainRoot.Width
            : _rightCurtain.SetWidth - 2 * ExtraWidth;

        var leftPos = MathHelper.Lerp(_leftStartingX, leftTargetXPos, t);
        var rightPos = MathHelper.Lerp(_rightStartingX, rightTargetXPos, t);

        LayoutContainer.SetPosition(_leftCurtain, new Vector2(leftPos, 0));
        LayoutContainer.SetPosition(_rightCurtain, new Vector2(rightPos, 0));

        if (_accumulatedTime < _currentTargetTime)
            return;

        _accumulatedTime = 0f;

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
                _resCache.GetTexture("/Textures/_ES/Interface/Lobby/curtains-left.png"),
            Visible = false,
        };
        _rightCurtain = new TextureRect
        {
            Stretch = TextureRect.StretchMode.Scale,
            Texture =
                _resCache.GetTexture("/Textures/_ES/Interface/Lobby/curtains-right.png"),
            Visible = false,
        };
        _curtainRoot.AddChild(_leftCurtain);
        _curtainRoot.AddChild(_rightCurtain);
    }

    private void StartCurtainAnimation(bool toOpen, TimeSpan? animationTimeOverride = null)
    {
        if (!_showAnimation)
            return;

        if ((toOpen && _curtainState > LobbyCurtainState.Closing) ||
            (!toOpen && _curtainState < LobbyCurtainState.Opening))
            return;

        _curtainState = toOpen ? LobbyCurtainState.Opening : LobbyCurtainState.Closing;
        _currentTargetTime = animationTimeOverride is not null
            ? (float)animationTimeOverride.Value.TotalSeconds
            : (float)DefaultAnimationTime.TotalSeconds;

        Log.Info($"Playing curtain animation: {_curtainState} for {Math.Round(_currentTargetTime / 1000, 2)} seconds");

        _leftCurtain.SetWidth = (_curtainRoot.Width / 2) + ExtraWidth; // slightly larger than half the window?
        _leftCurtain.SetHeight = _curtainRoot.Height;
        _leftCurtain.Visible = true;

        _rightCurtain.SetWidth = (_curtainRoot.Width / 2) + ExtraWidth;
        _rightCurtain.SetHeight = _curtainRoot.Height;
        _rightCurtain.Visible = true;

        if (!toOpen)
        {
            LayoutContainer.SetPosition(_leftCurtain, new Vector2(-_leftCurtain.SetWidth, 0));
            LayoutContainer.SetPosition(_rightCurtain, new Vector2(_curtainRoot.Width, 0));
        }
        else
        {
            LayoutContainer.SetPosition(_leftCurtain, Vector2.Zero);
            LayoutContainer.SetPosition(_rightCurtain, new Vector2(_rightCurtain.SetWidth - 2 * ExtraWidth, 0));
        }

        _leftStartingX = _leftCurtain.Position.X;
        _rightStartingX = _rightCurtain.Position.X;
    }
}

public enum LobbyCurtainState : byte
{
    Closed = 0,
    Closing = 1,
    Opening = 2,
    Open = 3,
}
