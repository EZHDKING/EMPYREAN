// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// About / credits window. Required attribution to the creator (AGENT §2.1).
    /// </summary>
    public partial class AboutWindow : Win95Window
    {
        public AboutWindow()
            : base("About EMPYREAN", FontAwesome.Solid.InfoCircle)
        {
            Name = "About EMPYREAN";
            Size = new Vector2(360, 220);

            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Anchor = Anchor.TopCentre,
                Origin = Anchor.TopCentre,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Padding = new MarginPadding(14),
                Children = new Drawable[]
                {
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "EMPYREAN",
                        Font = OsuFont.GetFont(size: 30, weight: FontWeight.Black),
                        Colour = Win95.TITLE,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "osu!lazer, but better",
                        Font = OsuFont.GetFont(size: 14),
                        Colour = Win95.TEXT,
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "Creator: EZHD KING",
                        Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                        Colour = Win95.TEXT,
                        Margin = new MarginPadding { Top = 8 },
                    },
                    new OsuSpriteText
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Text = "Maximum FPS. Lowest latency. Every frame matters.",
                        Font = OsuFont.GetFont(size: 12),
                        Colour = Win95.SHADOW,
                    },
                },
            });

            Add(Win95Button.Text("OK", () => { OnClose?.Invoke(); Expire(); }, 72, 24).With(b =>
            {
                b.Anchor = Anchor.BottomCentre;
                b.Origin = Anchor.BottomCentre;
                b.Margin = new MarginPadding { Bottom = 10 };
            }));
        }
    }
}
