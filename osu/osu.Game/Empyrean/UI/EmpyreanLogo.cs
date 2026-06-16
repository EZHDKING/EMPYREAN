// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Colour;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// The EMPYREAN logo — a 1980s vaporwave / early-Apple-and-Windows scene rendered with
    /// FLAT primitives only. There is deliberately no shader, no blur, no particle system and
    /// no per-frame animation: a deep-purple sky, a banded sun (discrete horizontal bars), a
    /// static perspective grid (a handful of lines), and the wordmark with a cyan/magenta
    /// offset for the classic chromatic look (two static text copies, not a shader).
    ///
    /// Vertex-colour gradients (<see cref="ColourInfo.GradientVertical"/>) are free — they cost
    /// nothing beyond the quad already being drawn — so the "gradient" aesthetic is achieved
    /// without any of the per-frame animated-gradient cost PROJECT.md §5.1 forbids.
    ///
    /// The whole composition is static once laid out, so it never invalidates (§5.3).
    /// </summary>
    public partial class EmpyreanLogo : Container
    {
        public EmpyreanLogo()
        {
            AutoSizeAxes = Axes.None;
            Size = new Vector2(520, 360);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            var children = new List<Drawable>
            {
                // Sky: a single vertical gradient quad (free).
                new Box
                {
                    RelativeSizeAxes = Axes.Both,
                    Colour = ColourInfo.GradientVertical(Win95.VW_NIGHT, Win95.VW_INDIGO),
                },
            };

            // Sun: a stack of horizontal bars over a base disc-ish square. Classic banded look.
            var sun = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Y = -40,
                Size = new Vector2(180),
                Masking = true,
                CornerRadius = 90,
                Children = new Drawable[]
                {
                    new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Colour = ColourInfo.GradientVertical(Win95.VW_SUN_TOP, Win95.VW_SUN_BOT),
                    },
                },
            };

            // Cut the lower half of the sun into bars by overlaying thin night-coloured strips
            // (cheaper and crisper than alpha tricks).
            for (int i = 0; i < 6; i++)
            {
                sun.Add(new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 4,
                    Y = 90 + i * 14, // lower half only
                    Colour = Win95.VW_NIGHT,
                });
            }

            children.Add(sun);

            // Perspective grid: a few static lines fanning toward a horizon. All thin boxes.
            var grid = new Container
            {
                Anchor = Anchor.BottomCentre,
                Origin = Anchor.BottomCentre,
                RelativeSizeAxes = Axes.X,
                Height = 120,
                Masking = true,
            };

            // horizontals (get denser toward the horizon)
            for (int i = 0; i < 6; i++)
            {
                grid.Add(new Box
                {
                    RelativeSizeAxes = Axes.X,
                    Height = 2,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Y = -(i * i * 3f),
                    Colour = Win95.VW_GRID,
                });
            }

            // verticals fanning out
            for (int i = -5; i <= 5; i++)
            {
                grid.Add(new Box
                {
                    Width = 2,
                    Height = 120,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    X = i * 55,
                    Shear = new Vector2(i * 0.06f, 0),
                    Colour = Win95.VW_GRID,
                });
            }

            children.Add(grid);

            // Wordmark with chromatic offset (cyan behind, magenta behind, white on top).
            var word = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Y = 30,
                AutoSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    chromaCopy(Win95.VW_CYAN, new Vector2(-3, 0)),
                    chromaCopy(Win95.VW_MAGENTA, new Vector2(3, 0)),
                    chromaCopy(Color4.White, Vector2.Zero),
                },
            };
            children.Add(word);

            // Tagline + creator credit (PROJECT.md §2.1).
            children.Add(new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Y = 70,
                Text = "EZHD KING",
                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
                Colour = Win95.VW_CYAN,
                Spacing = new Vector2(6, 0),
            });

            Children = children;
        }

        private static Drawable chromaCopy(Color4 colour, Vector2 offset) => new OsuSpriteText
        {
            Anchor = Anchor.Centre,
            Origin = Anchor.Centre,
            Position = offset,
            Text = "EMPYREAN",
            Font = OsuFont.GetFont(size: 56, weight: FontWeight.Black),
            Colour = colour,
            Spacing = new Vector2(8, 0),
        };
    }
}
