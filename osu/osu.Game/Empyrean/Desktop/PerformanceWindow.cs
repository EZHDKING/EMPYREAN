// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Drawing;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osu.Game.Configuration;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// EZHD Upscaler — sets the game's real render resolution as a percentage of your native display
    /// resolution, then the display upscales it. Scaling native by a percentage preserves the aspect
    /// ratio, so there are no black bars. The chosen resolution is only applied when you release the
    /// slider (so dragging doesn't flash the display on every tick). Also exposes EZHDSR sharpening.
    /// </summary>
    public partial class PerformanceWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private FrameworkConfigManager frameworkConfig { get; set; }

        [Resolved(canBeNull: true)]
        private OsuConfigManager osuConfig { get; set; }

        [Resolved(canBeNull: true)]
        private GameHost host { get; set; }

        private readonly Bindable<Size> sizeFullscreen = new Bindable<Size>();
        private readonly Bindable<Size> windowedSize = new Bindable<Size>();

        // 1..99 percent of native resolution. This is the slider's live value.
        private readonly BindableInt scalePercent = new BindableInt(50)
        {
            MinValue = 1,
            MaxValue = 99,
        };

        private readonly Bindable<bool> sharpen = new Bindable<bool>();

        private OsuSpriteText percentLabel;
        private OsuSpriteText resolutionLabel;

        public PerformanceWindow()
            : base("EZHD Upscaler", FontAwesome.Solid.Trophy)
        {
            Name = "EZHD Upscaler";
            Size = new Vector2(480, 360);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 10),
                        Padding = new MarginPadding(14),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText { Text = "EZHD Upscaler", Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold), Colour = Win95.TITLE },
                            new OsuSpriteText
                            {
                                Text = "Render at a percentage of your native resolution, then upscale.\nLower = the GPU draws far fewer pixels = big FPS gain.\nApplies when you release the slider. Use Fullscreen mode.",
                                Font = OsuFont.GetFont(size: 13),
                                Colour = Win95.TEXT,
                                AllowMultiline = true,
                                RelativeSizeAxes = Axes.X,
                            },
                            new OsuSpriteText { Text = "Render scale (% of native):", Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT, Margin = new MarginPadding { Top = 4 } },

                            // Slider in a Win95 sunken field. TransferValueOnCommit => the bound value
                            // (and thus the resolution change) only updates on release, not per drag
                            // tick, so the display doesn't flash repeatedly while dragging.
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 30,
                                Children = new Drawable[]
                                {
                                    new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.WORKSPACE },
                                    new Win95Bevel(Win95Bevel.Style.Field),
                                    new BasicSliderBar<int>
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(3),
                                        Current = scalePercent,
                                        TransferValueOnCommit = true,
                                        BackgroundColour = Win95.SHADOW,
                                        SelectionColour = Win95.TITLE,
                                    },
                                },
                            },

                            percentLabel = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold), Colour = Win95.TITLE },
                            resolutionLabel = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT },

                            // Quick presets.
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(6, 0),
                                Margin = new MarginPadding { Top = 2 },
                                Children = new Drawable[]
                                {
                                    preset("10%", 10),
                                    preset("25%", 25),
                                    preset("50%", 50),
                                    preset("75%", 75),
                                    preset("100%", 99),
                                },
                            },

                            new Win95CheckRow("EZHDSR sharpening (recovers edge clarity)", sharpen),
                        },
                    },
                },
            });

            sharpen.BindValueChanged(v =>
            {
                osuConfig?.SetValue(OsuSetting.EmpyreanSharpen, v.NewValue);
            });

            if (frameworkConfig != null)
            {
                frameworkConfig.BindWith(FrameworkSetting.SizeFullscreen, sizeFullscreen);
                frameworkConfig.BindWith(FrameworkSetting.WindowedSize, windowedSize);
            }

            if (osuConfig != null)
            {
                sharpen.Value = osuConfig.Get<bool>(OsuSetting.EmpyreanSharpen);
                int storedPct = (int)Math.Round(osuConfig.Get<double>(OsuSetting.EmpyreanRenderScale) * 100);
                scalePercent.Value = Math.Clamp(storedPct, 1, 99);
            }

            // Fires on commit (slider release) because of TransferValueOnCommit.
            scalePercent.BindValueChanged(e =>
            {
                applyScale(e.NewValue);
            });

            updateLabels();
        }

        private Win95Button preset(string label, int pct)
        {
            var b = new Win95Button { Size = new Vector2(70, 26) };
            b.Action = () =>
            {
                scalePercent.Value = pct;
                applyScale(pct);
            };
            b.Add(new OsuSpriteText { Anchor = Anchor.Centre, Origin = Anchor.Centre, Text = label, Font = OsuFont.GetFont(size: 12), Colour = Win95.TEXT });
            return b;
        }

        private (int w, int h) nativeSize()
        {
            // Native display resolution from the window's current display; fall back to a sane value.
            var bounds = host?.Window?.CurrentDisplayBindable.Value?.Bounds;
            if (bounds != null && bounds.Value.Width > 0)
                return (bounds.Value.Width, bounds.Value.Height);

            return (1920, 1080);
        }

        private void applyScale(int pct)
        {
            if (frameworkConfig == null)
                return;

            var (nw, nh) = nativeSize();

            // Scale native by the percentage — preserves aspect ratio (no letterbox black bars).
            int w = Math.Max(320, (int)Math.Round(nw * pct / 100.0));
            int h = Math.Max(240, (int)Math.Round(nh * pct / 100.0));

            sizeFullscreen.Value = new Size(w, h);
            windowedSize.Value = new Size(w, h);

            // Persist as a 0..1 fraction in the osu config too.
            osuConfig?.SetValue(OsuSetting.EmpyreanRenderScale, pct / 100.0);

            updateLabels();
        }

        private void updateLabels()
        {
            if (percentLabel == null)
                return;

            percentLabel.Text = $"{scalePercent.Value}% of native";

            var (nw, nh) = nativeSize();
            int w = Math.Max(320, (int)Math.Round(nw * scalePercent.Value / 100.0));
            int h = Math.Max(240, (int)Math.Round(nh * scalePercent.Value / 100.0));
            resolutionLabel.Text = $"≈ {w} x {h}  (native {nw} x {nh})";
        }
    }
}
