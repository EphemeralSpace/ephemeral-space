using System.Numerics;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

// ES MODIFIED : right-aligned main viewport instead of centered

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    ///     Wrapper for <see cref="ScalingViewport"/> that listens to configuration variables.
    ///     Also does NN-snapping within tolerances.
    /// </summary>
    public sealed class MainViewport : UIWidget
    {
        [Dependency] private readonly IConfigurationManager _cfg = default!;
        [Dependency] private readonly ViewportManager _vpManager = default!;

        public ScalingViewport Viewport { get; }

        public MainViewport()
        {
            IoCManager.InjectDependencies(this);

            Viewport = new ScalingViewport
            {
                AlwaysRender = true,
                RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt,
                MouseFilter = MouseFilterMode.Stop
            };

            AddChild(Viewport);

            _cfg.OnValueChanged(CCVars.ViewportScalingFilterMode, _ => UpdateCfg(), true);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _vpManager.AddViewport(this);
        }

        protected override void ExitedTree()
        {
            base.ExitedTree();

            _vpManager.RemoveViewport(this);
        }

        public void UpdateCfg()
        {
            var stretch = _cfg.GetCVar(CCVars.ViewportStretch);
            var renderScaleUp = _cfg.GetCVar(CCVars.ViewportScaleRender);
            var fixedFactor = _cfg.GetCVar(CCVars.ViewportFixedScaleFactor);
            var verticalFit = _cfg.GetCVar(CCVars.ViewportVerticalFit);
            var filterMode = _cfg.GetCVar(CCVars.ViewportScalingFilterMode);

            if (stretch)
            {
                var snapFactor = CalcSnappingFactor();
                if (snapFactor == null)
                {
                    // Did not find a snap, enable stretching.
                    Viewport.FixedStretchSize = null;
                    Viewport.StretchMode = filterMode switch
                    {
                        "nearest" => ScalingViewportStretchMode.Nearest,
                        "bilinear" => ScalingViewportStretchMode.Bilinear,
                        _ => ScalingViewportStretchMode.Nearest
                    };
                    Viewport.IgnoreDimension = verticalFit ? ScalingViewportIgnoreDimension.Horizontal : ScalingViewportIgnoreDimension.None;

                    if (renderScaleUp)
                    {
                        Viewport.RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt;
                    }
                    else
                    {
                        Viewport.RenderScaleMode = ScalingViewportRenderScaleMode.Fixed;
                        Viewport.FixedRenderScale = 1;
                    }

                    return;
                }

                // Found snap, set fixed factor and run non-stretching code.
                fixedFactor = snapFactor.Value;
            }

            Viewport.FixedStretchSize = Viewport.ViewportSize * fixedFactor;
            Viewport.StretchMode = ScalingViewportStretchMode.Nearest;

            if (renderScaleUp)
            {
                Viewport.RenderScaleMode = ScalingViewportRenderScaleMode.Fixed;
                Viewport.FixedRenderScale = fixedFactor;
            }
            else
            {
                // Snapping but forced to render scale at scale 1 so...
                // At least we can NN.
                Viewport.RenderScaleMode = ScalingViewportRenderScaleMode.Fixed;
                Viewport.FixedRenderScale = 1;
            }
        }

        private int? CalcSnappingFactor()
        {
            // Margin tolerance is tolerance of "the window is too big"
            // where we add a margin to the viewport to make it fit.
            var cfgToleranceMargin = _cfg.GetCVar(CCVars.ViewportSnapToleranceMargin);
            // Clip tolerance is tolerance of "the window is too small"
            // where we are clipping the viewport to make it fit.
            var cfgToleranceClip = _cfg.GetCVar(CCVars.ViewportSnapToleranceClip);

            var cfgVerticalFit = _cfg.GetCVar(CCVars.ViewportVerticalFit);

            // erm
            if (Root == null)
                return null;

            // Instead of all that, we just snap to the largest integer scale that fits.
            // If that (pre-clamp) scale is <1, we return null and let the scaling logic handle it.
            // TODO: This should probably enforce margins around the viewport.
            var possibleSize = ((Vector2)Root.PixelSize) / ((Vector2)Viewport.ViewportSize);

            var minPossible = Math.Min(possibleSize.X, possibleSize.Y);

            if (minPossible >= 1)
            {
                return (int)Math.Floor(minPossible);
            }

            // uhhh too tiny try again later
            return null;
        }

        protected override Vector2 MeasureOverride(Vector2 availableSize)
        {
            UpdateCfg();

            return base.MeasureOverride(availableSize);
        }

        protected override void Resized()
        {
            base.Resized();

            UpdateCfg();
        }
    }
}
