using System.Numerics;
using Content.Client.Viewport;
using Content.Client.UserInterface.Screens;
using Content.Shared.CCVar;
using Robust.Client.UserInterface;
using Robust.Client.UserInterface.Controls;
using Robust.Shared.Configuration;

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
        [Dependency] private readonly IUserInterfaceManager _uiManager = default!;

        public ScalingViewport Viewport { get; }

        // Sunrise-Start
        /// <summary>When true (Separated), forces <see cref="ScalingViewportIgnoreDimension.None"/> to fit
        /// viewport in container. When false (Default), <see cref="ScalingViewportIgnoreDimension.Horizontal"/>
        /// fills screen height with black bars only on ultrawide.</summary>
        public bool Force169Fit { get; set; }

        /// <summary>Skips CalcSnappingFactor so IgnoreDimension is always used instead of FixedStretchSize.
        /// Required for 16:9 fill modes — snap would produce centered bars on all sides.</summary>
        public bool Is169Mode { get; set; }
        // Sunrise-End

        public MainViewport()
        {
            IoCManager.InjectDependencies(this);

            Viewport = new ScalingViewport
            {
                AlwaysRender = true,
                RenderScaleMode = ScalingViewportRenderScaleMode.CeilInt,
                MouseFilter = MouseFilterMode.Stop,
                HorizontalExpand = true,
                VerticalExpand = true
            };

            AddChild(Viewport);

            _cfg.OnValueChanged(CCVars.ViewportScalingFilterMode, _ => UpdateCfg(), true);
        }

        protected override void EnteredTree()
        {
            base.EnteredTree();

            _vpManager.AddViewport(this);
            UpdateCfg();
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
                var snapFactor = (verticalFit || Is169Mode) ? (int?) null : CalcSnappingFactor(); // Sunrise-Edit: skip snap in 16:9 fill modes
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
                    // Sunrise-Start
                    var localVerticalFit = _cfg.GetCVar(CCVars.ViewportVerticalFit) && _cfg.GetCVar(CCVars.ViewportStretch);
                    if (localVerticalFit)
                    {
                        const int vpH = 480;
                        const float refAspect = 16f / 9f;
                        const float maxWidthAspect = 2.1f;

                        var ourSizeX = PixelSize.X > 0 ? PixelSize.X : (_uiManager.ActiveScreen?.PixelSize.X ?? 0);
                        var ourSizeY = PixelSize.Y > 0 ? PixelSize.Y : (_uiManager.ActiveScreen?.PixelSize.Y ?? 0);

                        if (ourSizeY > 0)
                        {
                            if ((Force169Fit || _uiManager.ActiveScreen is SeparatedChatGameScreen) && ourSizeX > 0)
                            {
                                var fullW = _uiManager.ActiveScreen?.PixelSize.X ?? ourSizeX;
                                var fullH = _uiManager.ActiveScreen?.PixelSize.Y ?? ourSizeY;
                                var fullAspect = fullH > 0 ? (float)fullW / fullH : refAspect;

                                var clampedFullAspect = Math.Max(fullAspect, refAspect);
                                clampedFullAspect = Math.Min(clampedFullAspect, maxWidthAspect);

                                var refW = (int)Math.Ceiling(vpH * clampedFullAspect) + 1;
                                var newH = (int)Math.Ceiling((double)refW * ourSizeY / ourSizeX);

                                var newSize = new Vector2i(refW, Math.Max(vpH, newH));
                                if (Viewport.ViewportSize != newSize)
                                    Viewport.ViewportSize = newSize;

                                Viewport.IgnoreDimension = ScalingViewportIgnoreDimension.None;
                            }
                            else if (ourSizeX > 0)
                            {
                                var screenAspect = (float)ourSizeX / ourSizeY;

                                if (screenAspect <= maxWidthAspect)
                                {
                                    var newW = (int)Math.Ceiling(vpH * screenAspect) + 1;
                                    var newSize = new Vector2i(newW, vpH);
                                    if (Viewport.ViewportSize != newSize)
                                        Viewport.ViewportSize = newSize;
                                    Viewport.IgnoreDimension = ScalingViewportIgnoreDimension.Horizontal;
                                }
                                else
                                {
                                    var newW = (int)Math.Ceiling(vpH * refAspect);
                                    var newSize = new Vector2i(newW, vpH);
                                    if (Viewport.ViewportSize != newSize)
                                        Viewport.ViewportSize = newSize;
                                    Viewport.IgnoreDimension = ScalingViewportIgnoreDimension.None;
                                }
                            }
                        }
                    }
                    else
                    {
                        Viewport.IgnoreDimension = localVerticalFit
                            ? ScalingViewportIgnoreDimension.Horizontal
                            : ScalingViewportIgnoreDimension.None;
                    }
                    // Sunrise-End

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

            // Calculate if the viewport, when rendered at an integer scale,
            // is close enough to the control size to enable "snapping" to NN,
            // potentially cutting a tiny bit off/leaving a margin.
            //
            // Idea here is that if you maximize the window at 1080p or 1440p
            // we are close enough to an integer scale (2x and 3x resp) that we should "snap" to it.

            // Just do it iteratively.
            // I'm sure there's a smarter approach that needs one try with math but I'm dumb.
            for (var i = 1; i <= 10; i++)
            {
                var toleranceMargin = i * cfgToleranceMargin;
                var toleranceClip = i * cfgToleranceClip;
                var scaled = (Vector2) Viewport.ViewportSize * i;
                var (dx, dy) = PixelSize - scaled;

                // The rule for which snap fits is that at LEAST one axis needs to be in the tolerance size wise.
                // One axis MAY be larger but not smaller than tolerance.
                // Obviously if it's too small it's bad, and if it's too big on both axis we should stretch up.
                // Additionally, if the viewport's supposed  to be vertically fit, then the horizontal scale should just be ignored where appropriate.
                if ((Fits(dx) || cfgVerticalFit) && Fits(dy) || !cfgVerticalFit && Fits(dx) && Larger(dy) || Larger(dx) && Fits(dy))
                {
                    // Found snap that fits.
                    return i;
                }

                bool Larger(float a)
                {
                    return a > toleranceMargin;
                }

                bool Fits(float a)
                {
                    return a <= toleranceMargin && a >= -toleranceClip;
                }
            }

            return null;
        }

        protected override void Resized()
        {
            base.Resized();

            UpdateCfg();
        }
    }
}
