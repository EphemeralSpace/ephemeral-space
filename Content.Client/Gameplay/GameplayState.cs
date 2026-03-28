using System.Numerics;
using Content.Client._ES.Screens;
using Content.Client.Changelog;
using Content.Client.Hands;
using Content.Client.UserInterface.Controls;
using Content.Client.UserInterface.Screens;
using Content.Client.UserInterface.Systems.Gameplay;
using Content.Client.Viewport;
using Content.Shared._ES.Stagehand.Components;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.Player;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Client.UserInterface.CustomControls;
using Robust.Shared.Configuration;
using Robust.Shared.Timing;

// ES MODIFIED: edited to support switching screens

namespace Content.Client.Gameplay
{
    [Virtual]
    public class GameplayState : GameplayStateBase, IMainViewportState
    {
        [Dependency] private readonly IEyeManager _eyeManager = default!;
        [Dependency] private readonly IOverlayManager _overlayManager = default!;
        [Dependency] private readonly IGameTiming _gameTiming = default!;
        [Dependency] private readonly ChangelogManager _changelog = default!;
        [Dependency] private readonly IConfigurationManager _configurationManager = default!;
        [Dependency] private readonly IPlayerManager _player = default!;
        [Dependency] private readonly IEntityManager _ent = default!;

        private FpsCounter _fpsCounter = default!;
        private Label _version = default!;

        public MainViewport Viewport => UserInterfaceManager.ActiveScreen!.GetWidget<MainViewport>()!;

        private GameplayStateScreenType _screenType = GameplayStateScreenType.Performer;

        private readonly GameplayStateLoadController _loadController;

        public GameplayState()
        {
            IoCManager.InjectDependencies(this);

            _loadController = UserInterfaceManager.GetUIController<GameplayStateLoadController>();
        }

        protected override void Startup()
        {
            base.Startup();

            if (_player.LocalEntity is { } entity && _ent.HasComponent<ESStagehandComponent>(entity))
                _screenType = GameplayStateScreenType.Stagehand;

            LoadMainScreen();
            _configurationManager.OnValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);

            // Add the hand-item overlay.
            _overlayManager.AddOverlay(new ShowHandItemOverlay());

            // FPS counter.
            // yeah this can just stay here, whatever
            _fpsCounter = new FpsCounter(_gameTiming);
            UserInterfaceManager.PopupRoot.AddChild(_fpsCounter);
            _fpsCounter.Visible = _configurationManager.GetCVar(CCVars.HudFpsCounterVisible);
            _configurationManager.OnValueChanged(CCVars.HudFpsCounterVisible, (show) => { _fpsCounter.Visible = show; });

            // Version number watermark.
            _version = new Label();
            _version.FontColorOverride = Color.FromHex("#FFFFFF20");
            _version.Text = _changelog.GetClientVersion();
            UserInterfaceManager.PopupRoot.AddChild(_version);
            _configurationManager.OnValueChanged(CCVars.HudVersionWatermark, (show) => { _version.Visible = VersionVisible(); }, true);
            _configurationManager.OnValueChanged(CCVars.ForceClientHudVersionWatermark, (show) => { _version.Visible = VersionVisible(); }, true);
            // TODO make this centered or something
            LayoutContainer.SetPosition(_version, new Vector2(70, 0));
        }

        // This allows servers to force the watermark on clients
        private bool VersionVisible()
        {
            var client = _configurationManager.GetCVar(CCVars.HudVersionWatermark);
            var server = _configurationManager.GetCVar(CCVars.ForceClientHudVersionWatermark);
            return client || server;
        }

        protected override void Shutdown()
        {
            _overlayManager.RemoveOverlay<ShowHandItemOverlay>();

            base.Shutdown();
            // Clear viewport to some fallback, whatever.
            _eyeManager.MainViewport = UserInterfaceManager.MainViewport;
            _fpsCounter.Dispose();
            UserInterfaceManager.ClearWindows();
            _configurationManager.UnsubValueChanged(CCVars.UILayout, ReloadMainScreenValueChange);
            UnloadMainScreen();
        }

        private void ReloadMainScreenValueChange(string _)
        {
            ReloadMainScreen();
        }

        public void ReloadMainScreen()
        {
            if (UserInterfaceManager.ActiveScreen?.GetWidget<MainViewport>() == null)
            {
                return;
            }

            UnloadMainScreen();
            LoadMainScreen();
        }

        public void SetScreenType(GameplayStateScreenType type)
        {
            _screenType = type;
            ReloadMainScreen();
        }

        private void UnloadMainScreen()
        {
            _loadController.UnloadScreen();
            UserInterfaceManager.UnloadScreen();
        }

        private void LoadMainScreen()
        {
            switch (_screenType)
            {
                case GameplayStateScreenType.Performer:
                    UserInterfaceManager.LoadScreen<PerformerGameScreen>();
                    break;
                case GameplayStateScreenType.Stagehand:
                    UserInterfaceManager.LoadScreen<StagehandGameScreen>();
                    break;
            }

            _loadController.LoadScreen();
        }

        protected override void OnKeyBindStateChanged(ViewportBoundKeyEventArgs args)
        {
            if (args.Viewport == null)
                base.OnKeyBindStateChanged(new ViewportBoundKeyEventArgs(args.KeyEventArgs, Viewport.Viewport));
            else
                base.OnKeyBindStateChanged(args);
        }
    }
}

/// <summary>
///     Defines which screen this state should load
/// </summary>
public enum GameplayStateScreenType
{
    /// <summary>
    ///     Screen used for regular players
    /// </summary>
    Performer,

    /// <summary>
    ///     Screen used for stagehands
    /// </summary>
    Stagehand
}
