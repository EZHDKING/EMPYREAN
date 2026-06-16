// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Configuration;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// FPS Settings — a Win95 control panel for the on-screen FPS counter: toggle it, resize it
    /// (1x–5x) and pick which screen corner it sits in. All changes apply live.
    /// </summary>
    public partial class FpsSettingsWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private OsuConfigManager config { get; set; }

        private readonly Bindable<bool> show = new Bindable<bool>(true);
        private readonly BindableDouble scale = new BindableDouble(2.5)
        {
            MinValue = 1.0,
            MaxValue = 5.0,
            Precision = 0.1,
        };
        private readonly BindableInt corner = new BindableInt(0)
        {
            MinValue = 0,
            MaxValue = 3,
        };

        private OsuSpriteText scaleLabel;
        private OsuSpriteText cornerLabel;

        public FpsSettingsWindow()
            : base("FPS Settings", FontAwesome.Solid.Trophy)
        {
            Name = "FPS Settings";
            Size = new Vector2(460, 360);
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
                            new OsuSpriteText { Text = "FPS Counter", Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold), Colour = Win95.TITLE },
                            new Win95CheckRow("Show FPS counter", show),

                            new OsuSpriteText { Text = "Size:", Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT, Margin = new MarginPadding { Top = 6 } },
                            new Container
                            {
                                RelativeSizeAxes = Axes.X,
                                Height = 30,
                                Children = new Drawable[]
                                {
                                    new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.WORKSPACE },
                                    new Win95Bevel(Win95Bevel.Style.Field),
                                    new BasicSliderBar<double>
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                        Padding = new MarginPadding(3),
                                        Current = scale,
                                        BackgroundColour = Win95.SHADOW,
                                        SelectionColour = Win95.TITLE,
                                    },
                                },
                            },
                            scaleLabel = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TITLE },

                            new OsuSpriteText { Text = "Position:", Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT, Margin = new MarginPadding { Top = 6 } },
                            new FillFlowContainer
                            {
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(6, 0),
                                Children = new Drawable[]
                                {
                                    cornerButton("Top-Left", 1),
                                    cornerButton("Top-Right", 0),
                                    cornerButton("Bottom-Left", 3),
                                    cornerButton("Bottom-Right", 2),
                                },
                            },
                            cornerLabel = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT },
                        },
                    },
                },
            });

            if (config != null)
            {
                config.BindWith(OsuSetting.ShowFpsDisplay, show);
                config.BindWith(OsuSetting.EmpyreanFpsScale, scale);
                config.BindWith(OsuSetting.EmpyreanFpsCorner, corner);
            }

            scale.BindValueChanged(_ => updateLabels(), true);
            corner.BindValueChanged(_ => updateLabels(), true);
        }

        private Win95Button cornerButton(string label, int value)
        {
            var b = new Win95Button { Size = new Vector2(100, 28) };
            b.Action = () => corner.Value = value;
            b.Add(new OsuSpriteText { Anchor = Anchor.Centre, Origin = Anchor.Centre, Text = label, Font = OsuFont.GetFont(size: 11), Colour = Win95.TEXT });
            return b;
        }

        private void updateLabels()
        {
            if (scaleLabel == null)
                return;

            scaleLabel.Text = $"{scale.Value:0.0}x size";

            string[] names = { "Top-Right", "Top-Left", "Bottom-Right", "Bottom-Left" };
            int c = Math.Clamp(corner.Value, 0, 3);
            cornerLabel.Text = $"Corner: {names[c]}";
        }
    }
}
