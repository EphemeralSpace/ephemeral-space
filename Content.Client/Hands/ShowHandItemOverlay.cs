using System.Numerics;
using Content.Client.Cooldown;
using Content.Client.Hands.Systems;
using Content.Shared.CCVar;
using Content.Shared.Timing;
using Robust.Client.GameObjects;
using Robust.Client.Graphics;
using Robust.Client.Input;
using Robust.Client.UserInterface;
using Robust.Shared.Configuration;
using Robust.Shared.Enums;
using Robust.Shared.Graphics;
using Robust.Shared.Map;
using Direction = Robust.Shared.Maths.Direction;

namespace Content.Client.Hands
{
    public sealed partial class ShowHandItemOverlay : Overlay
    {
        [Dependency] private IUserInterfaceManager _ui = default!;
        [Dependency] private IConfigurationManager _cfg = default!;
        [Dependency] private IInputManager _inputManager = default!;
        [Dependency] private IClyde _clyde = default!;
        [Dependency] private IEntityManager _entMan = default!;

        private HandsSystem? _hands;
        private UseDelaySystem? _delay;
        private readonly IRenderTexture _renderBackbuffer;
        private readonly CooldownGraphic _cooldownGraphic;

        public override OverlaySpace Space => OverlaySpace.ScreenSpace;

        public Texture? IconOverride;
        public EntityUid? EntityOverride;

        public ShowHandItemOverlay()
        {
            IoCManager.InjectDependencies(this);

            _cooldownGraphic = new()
            {
                SetSize = new(64, 64)
            };

            // mild jank to get it to size correctly since we are rendering this directly.
            _cooldownGraphic.Arrange(UIBox2.FromDimensions(Vector2.Zero, new Vector2(64, 64)));

            _renderBackbuffer = _clyde.CreateRenderTarget(
                (64, 64),
                new RenderTargetFormatParameters(RenderTargetColorFormat.Rgba8Srgb, true),
                new TextureSampleParameters
                {
                    Filter = true
                }, nameof(ShowHandItemOverlay));
        }

        protected override void DisposeBehavior()
        {
            base.DisposeBehavior();

            _renderBackbuffer.Dispose();
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_cfg.GetCVar(CCVars.HudHeldItemShow))
                return false;

            return base.BeforeDraw(in args);
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            var mousePos = _inputManager.MouseScreenPosition;

            // Offscreen
            if (mousePos.Window == WindowId.Invalid)
                return;

            var screen = args.ScreenHandle;
            screen.SetTransform(Matrix3x2.Identity);
            var offset = _cfg.GetCVar(CCVars.HudHeldItemOffset);
            var offsetVec = new Vector2(offset, offset);

            if (IconOverride != null)
            {
                screen.DrawTexture(IconOverride, mousePos.Position - IconOverride.Size / 2 + offsetVec, Color.White.WithAlpha(0.75f));
                return;
            }

            _hands ??= _entMan.System<HandsSystem>();
            var handEntity = _hands.GetActiveHandEntity();

            if (handEntity == null || !_entMan.TryGetComponent(handEntity, out SpriteComponent? sprite))
                return;

            var halfSize = _renderBackbuffer.Size / 2;
            var vpControl = args.ViewportControl as Control;
            var uiScale = vpControl?.UIScale ?? 1f;

            screen.RenderInRenderTarget(_renderBackbuffer, () =>
            {
                screen.DrawEntity(handEntity.Value, halfSize, new Vector2(1f, 1f) * uiScale, Angle.Zero, Angle.Zero, Direction.South, sprite);
            }, Color.Transparent);

            screen.DrawTexture(_renderBackbuffer.Texture, mousePos.Position - halfSize + offsetVec, Color.White.WithAlpha(0.75f));

            // render cooldown circle graphic
            if (!_entMan.TryGetComponent<UseDelayComponent>(handEntity, out var delay))
                return;

            _delay ??= _entMan.System<UseDelaySystem>();
            var cooldown = _delay.GetLastEndingDelay((handEntity.Value, delay));
            if (cooldown.StartTime == cooldown.EndTime || cooldown.Length == TimeSpan.Zero)
            {
                return;
            }

            _cooldownGraphic.FromTime(cooldown.StartTime, cooldown.EndTime);
            // add child temporarily to get the ui scale to work
            vpControl?.AddChild(_cooldownGraphic);
            _ui.RenderControl(args.RenderHandle, _cooldownGraphic, (Vector2i) (mousePos.Position - (_cooldownGraphic.PixelSize) / 2));
            vpControl?.RemoveChild(_cooldownGraphic);
        }
    }
}
