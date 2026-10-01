using Robust.Client.Graphics;
using Robust.Client.Player;
using Robust.Shared.Enums;
using Robust.Shared.Prototypes;
using Content.Shared.Eye.Blinding.Components;

namespace Content.Client.Eye.Blinding
{
    public sealed partial class BlurryVisionOverlay : Overlay
    {
        private static readonly ProtoId<ShaderPrototype> CircleShader = "CircleMask";

        [Dependency] private IEntityManager _entityManager = default!;
        [Dependency] private IPlayerManager _playerManager = default!;
        [Dependency] private IPrototypeManager _prototypeManager = default!;

        public override bool RequestScreenTexture => true;
        public override OverlaySpace Space => OverlaySpace.WorldSpace;
        private readonly ShaderInstance _circleMaskShader;
        private float _magnitude;
        private float _correctionPower = 2.0f;

        private const float NoMotion_Radius = 30.0f; // Base radius for the nomotion variant at its full strength
        private const float NoMotion_Pow = 0.2f; // Exponent for the nomotion variant's gradient
        private const float NoMotion_Max = 8.0f; // Max value for the nomotion variant's gradient
        private const float NoMotion_Mult = 0.75f; // Multiplier for the nomotion variant

        public BlurryVisionOverlay()
        {
            IoCManager.InjectDependencies(this);
            _circleMaskShader = _prototypeManager.Index(CircleShader).InstanceUnique();

            _circleMaskShader.SetParameter("CircleMinDist", 0.0f);
            _circleMaskShader.SetParameter("CirclePow", NoMotion_Pow);
            _circleMaskShader.SetParameter("CircleMax", NoMotion_Max);
            _circleMaskShader.SetParameter("CircleMult", NoMotion_Mult);
        }

        protected override bool BeforeDraw(in OverlayDrawArgs args)
        {
            if (!_entityManager.TryGetComponent(_playerManager.LocalSession?.AttachedEntity, out EyeComponent? eyeComp))
                return false;

            if (args.Viewport.Eye != eyeComp.Eye)
                return false;

            var playerEntity = _playerManager.LocalSession?.AttachedEntity;

            if (playerEntity == null)
                return false;

            if (!_entityManager.TryGetComponent<BlurryVisionComponent>(playerEntity, out var blurComp))
                return false;

            if (blurComp.Magnitude <= 0)
                return false;

            if (_entityManager.TryGetComponent<BlindableComponent>(playerEntity, out var blindComp)
                && blindComp.IsBlind)
                return false;

            _magnitude = blurComp.Magnitude;
            _correctionPower = blurComp.CorrectionPower;
            return true;
        }

        protected override void Draw(in OverlayDrawArgs args)
        {
            if (ScreenTexture == null)
                return;

            var playerEntity = _playerManager.LocalSession?.AttachedEntity;

            var worldHandle = args.WorldHandle;
            var viewport = args.WorldBounds;
            var strength = (float) Math.Pow(Math.Min(_magnitude / BlurryVisionComponent.MaxMagnitude, 1.0f), _correctionPower);

            var zoom = 1.0f;
            if (_entityManager.TryGetComponent<EyeComponent>(playerEntity, out var eyeComponent))
            {
                zoom = eyeComponent.Zoom.X;
            }

            _circleMaskShader.SetParameter("SCREEN_TEXTURE", ScreenTexture);
            _circleMaskShader.SetParameter("Zoom", zoom);
            _circleMaskShader.SetParameter("CircleRadius", NoMotion_Radius / strength);

            worldHandle.UseShader(_circleMaskShader);
            worldHandle.DrawRect(viewport, Color.White);
            worldHandle.UseShader(null);
        }
    }
}
