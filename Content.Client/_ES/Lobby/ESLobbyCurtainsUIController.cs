using System.Numerics;
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

    public LobbyCurtainState CurtainState { get; private set; } = LobbyCurtainState.Open;

    private LayoutContainer _curtainRoot = default!;
    private TextureRect _leftCurtain = default!;
    private TextureRect _rightCurtain = default!;

    private const int ExtraWidth = 100;

    private static readonly TimeSpan DefaultAnimationTime = TimeSpan.FromSeconds(1.5);
    private static readonly TimeSpan ClosedPanicOpenTime = TimeSpan.FromSeconds(10);
    private float _currentTargetTime;
    private float _accumulatedTime;
    private float _timeSpentClosed; // measured so we can panic-open the curtains if theyre closed for too long for some reason
    private float _leftStartingX;
    private float _rightStartingX;

    public override void Initialize()
    {
        base.Initialize();

        _conHost.RegisterCommand("togglelobbycurtains", "Toggles the lobby curtains animation", "togglelobbycurtains", (_, _, _) => StartCurtainAnimation(CurtainState < LobbyCurtainState.Opening));

        CreateCurtainControls();
    }

    public override void FrameUpdate(FrameEventArgs args)
    {
        base.FrameUpdate(args);

        if (CurtainState is LobbyCurtainState.Closed)
        {
            _timeSpentClosed += args.DeltaSeconds;
            if (_timeSpentClosed > ClosedPanicOpenTime.TotalSeconds)
            {
                Log.Info("Closed panic time exceeded: forcing curtains open.");
                StartCurtainAnimation(true, TimeSpan.FromSeconds(0.5));
                _timeSpentClosed = 0f;
                return;
            }
        }
        else
        {
            _timeSpentClosed = 0f;
        }

        if (CurtainState is not (LobbyCurtainState.Closing or LobbyCurtainState.Opening))
            return;

        _accumulatedTime += args.DeltaSeconds;

        var t = Easings.InOutQuad(Math.Clamp(_accumulatedTime / _currentTargetTime, 0f, 1f));

        var leftTargetXPos = CurtainState is LobbyCurtainState.Opening
            ? -_leftCurtain.SetWidth
            : 0;
        var rightTargetXPos = CurtainState is LobbyCurtainState.Opening
            ? _curtainRoot.Width
            : _rightCurtain.SetWidth - 2 * ExtraWidth;

        var leftPos = MathHelper.Lerp(_leftStartingX, leftTargetXPos, t);
        var rightPos = MathHelper.Lerp(_rightStartingX, rightTargetXPos, t);

        LayoutContainer.SetPosition(_leftCurtain, new Vector2(leftPos, 0));
        LayoutContainer.SetPosition(_rightCurtain, new Vector2(rightPos, 0));

        if (_accumulatedTime < _currentTargetTime)
            return;

        _accumulatedTime = 0f;

        CurtainState = CurtainState switch
        {
            LobbyCurtainState.Closing => LobbyCurtainState.Closed,
            LobbyCurtainState.Opening => LobbyCurtainState.Open,
            _ => CurtainState,
        };

        if (CurtainState == LobbyCurtainState.Open)
        {
            _leftCurtain.Visible = false;
            _rightCurtain.Visible = false;
        }
    }

    private bool IsAnimationDisabled()
    {
        return !_cfg.GetCVar(CCVars.GameLobbyCurtainAnimation) || _cfg.GetCVar(CCVars.ReducedMotion);
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

    /// <summary>
    /// starts a curtain animation, either opening or closing
    /// </summary>
    /// <param name="toOpen">whether to open or close the curtains</param>
    /// <param name="animationTimeOverride">the amount of time the animation should take</param>
    public void StartCurtainAnimation(bool toOpen, TimeSpan? animationTimeOverride = null)
    {
        if (IsAnimationDisabled())
            return;

        if ((toOpen && CurtainState > LobbyCurtainState.Closing) ||
            (!toOpen && CurtainState < LobbyCurtainState.Opening))
            return;

        CurtainState = toOpen ? LobbyCurtainState.Opening : LobbyCurtainState.Closing;
        _currentTargetTime = animationTimeOverride is not null
            ? (float)animationTimeOverride.Value.TotalSeconds
            : (float)DefaultAnimationTime.TotalSeconds;

        Log.Info($"Playing curtain animation: {CurtainState} for {Math.Round(_currentTargetTime, 2)} seconds");

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
