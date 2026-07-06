using System.Numerics;
using Content.Client.Viewport;
using Content.Shared.CCVar;
using Robust.Client.Graphics;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

namespace Content.Client.UserInterface.Controls
{
    /// <summary>
    ///     Wrapper for <see cref="ScalingViewport"/> that listens to configuration variables.
    ///     Also does NN-snapping within tolerances.
    /// </summary>
    public sealed partial class MainViewport : UIWidget
    {
        [Dependency] private IConfigurationManager _cfg = default!;

        public ScalingViewport Viewport { get; }

        private const int ViewportHeight = 15;

        // basically
        private const float MinSnapFillRatio = 0.85f;

        public MainViewport()
        {
            IoCManager.InjectDependencies(this);

            Viewport = new ScalingViewport
            {
                AlwaysRender = true,
                RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt,
                MouseFilter = MouseFilterMode.Stop,
                HorizontalExpand = true,
                VerticalExpand = true,
            };

            AddChild(Viewport);

            _cfg.OnValueChanged(CCVars.ViewportScalingFilterMode, _ => UpdateCfg());
            _cfg.OnValueChanged(CCVars.ViewportMaximumWidth, _ => UpdateCfg());
            _cfg.OnValueChanged(CCVars.ViewportStretch, _ => UpdateCfg());
            _cfg.OnValueChanged(CCVars.ViewportScaleRender, _ => UpdateCfg());
            _cfg.OnValueChanged(CCVars.ViewportFixedScaleFactor, _ => UpdateCfg());
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            UpdateCfg();
        }

        private void UpdateCfg()
        {
            var stretch = _cfg.GetCVar(CCVars.ViewportStretch);
            var renderScaleUp = _cfg.GetCVar(CCVars.ViewportScaleRender);
            var fixedFactor = _cfg.GetCVar(CCVars.ViewportFixedScaleFactor);
            var filterMode = _cfg.GetCVar(CCVars.ViewportScalingFilterMode);
            var width = _cfg.GetCVar(CCVars.ViewportMaximumWidth);

            Viewport.ViewportSize = (EyeManager.PixelsPerMeter * width, EyeManager.PixelsPerMeter * ViewportHeight);

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
                    Viewport.IgnoreDimension = ScalingViewportIgnoreDimension.Horizontal;

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
            // erm
            if (Root == null)
                return null;

            if (Viewport.ViewportSize.X <= 0 || Viewport.ViewportSize.Y <= 0)
                return null;

            var possibleSize = Root.PixelSize / (Vector2)Viewport.ViewportSize;
            var minPossible = Math.Min(possibleSize.X, possibleSize.Y);

            if (minPossible < 1)
                return null; // too tiny, always scale

            var flooredScale = (int)Math.Floor(minPossible);
            if (flooredScale < 1)
                return null;

            // if the desired integer scale doesnt fill up enough space
            // just scale normally
            var fillRatio = flooredScale / minPossible;

            if (fillRatio >= MinSnapFillRatio)
                return flooredScale;

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

        protected override void UIScaleChanged()
        {
            base.UIScaleChanged();

            UpdateCfg();
        }
    }
}
