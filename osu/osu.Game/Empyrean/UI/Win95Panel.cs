// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// EMPYREAN Windows-95 palette and shared visual constants (PROJECT.md §5).
    /// Flat surfaces, hard bevels, no translucency, no blur, no shadows.
    /// </summary>
    public static class Win95
    {
        // Classic Win95 system colours — exact tokens from the React95 "original" theme so the
        // look is indistinguishable from real Windows 95 (verified against react95).
        public static readonly Color4 FACE = Color4Extensions.FromHex("C6C6C6");      // material / button face
        public static readonly Color4 HILIGHT = Color4Extensions.FromHex("FEFEFE");    // borderLightest (brightest)
        public static readonly Color4 LIGHT = Color4Extensions.FromHex("DFDFDF");      // borderLight
        public static readonly Color4 SHADOW = Color4Extensions.FromHex("848584");     // borderDark
        public static readonly Color4 DKSHADOW = Color4Extensions.FromHex("0A0A0A");   // borderDarkest
        public static readonly Color4 TITLE = Color4Extensions.FromHex("060084");      // headerBackground (navy)
        public static readonly Color4 TITLE_INACTIVE = Color4Extensions.FromHex("7F787F");
        public static readonly Color4 TITLE_TEXT = Color4Extensions.FromHex("FEFEFE");
        public static readonly Color4 WORKSPACE = Color4Extensions.FromHex("008080");  // teal desktop
        public static readonly Color4 CANVAS = Color4Extensions.FromHex("FFFFFF");     // white field background
        public static readonly Color4 TEXT = Color4Extensions.FromHex("0A0A0A");       // canvasText
        public static readonly Color4 TEXT_DISABLED = Color4Extensions.FromHex("848584");
        public static readonly Color4 TERMINAL_BG = Color4.Black;
        public static readonly Color4 TERMINAL_FG = Color4Extensions.FromHex("C6C6C6");

        public const float BEVEL = 2f;
        public const float TITLE_HEIGHT = 18f;

        // EMPYREAN vaporwave accent palette (PROJECT.md §2 / §5: a 1980s Apple/early-Windows
        // aesthetic done with FLAT colour only — these are used as solid fills and gradient
        // *bands*, never as live shaders, blur, or animated gradients).
        public static readonly Color4 VW_MAGENTA = Color4Extensions.FromHex("FF6AD5");
        public static readonly Color4 VW_PINK = Color4Extensions.FromHex("FF71CE");
        public static readonly Color4 VW_PURPLE = Color4Extensions.FromHex("B967FF");
        public static readonly Color4 VW_CYAN = Color4Extensions.FromHex("05FFA1");
        public static readonly Color4 VW_BLUE = Color4Extensions.FromHex("01CDFE");
        public static readonly Color4 VW_INDIGO = Color4Extensions.FromHex("3B1E6D");
        public static readonly Color4 VW_NIGHT = Color4Extensions.FromHex("1A0633");      // deep purple sky
        public static readonly Color4 VW_SUN_TOP = Color4Extensions.FromHex("FFE56C");
        public static readonly Color4 VW_SUN_BOT = Color4Extensions.FromHex("FF2079");
        public static readonly Color4 VW_GRID = Color4Extensions.FromHex("FF2CA6");
    }

    /// <summary>
    /// A flat panel with the classic raised (or sunken) two-pixel Win95 bevel.
    /// Implemented with four <see cref="Box"/>es and a face — no masking, no shadow,
    /// no rounded corners. Cheap and static: it never invalidates after layout (§5.3).
    /// </summary>
    public partial class Win95Panel : Container
    {
        protected override Container<Drawable> Content => content;
        private readonly Container content;

        public Win95Panel(bool sunken = false)
        {
            Color4 topLeft = sunken ? Win95.SHADOW : Win95.HILIGHT;
            Color4 bottomRight = sunken ? Win95.HILIGHT : Win95.SHADOW;

            InternalChildren = new Drawable[]
            {
                // base face
                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                // top + left highlight
                new Box { RelativeSizeAxes = Axes.X, Height = Win95.BEVEL, Colour = topLeft, Anchor = Anchor.TopLeft, Origin = Anchor.TopLeft },
                new Box { RelativeSizeAxes = Axes.Y, Width = Win95.BEVEL, Colour = topLeft, Anchor = Anchor.TopLeft, Origin = Anchor.TopLeft },
                // bottom + right shadow
                new Box { RelativeSizeAxes = Axes.X, Height = Win95.BEVEL, Colour = bottomRight, Anchor = Anchor.BottomLeft, Origin = Anchor.BottomLeft },
                new Box { RelativeSizeAxes = Axes.Y, Width = Win95.BEVEL, Colour = bottomRight, Anchor = Anchor.TopRight, Origin = Anchor.TopRight },
                content = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(Win95.BEVEL),
                },
            };
        }
    }
}
