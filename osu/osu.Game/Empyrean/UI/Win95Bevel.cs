// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osuTK.Graphics;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// The authentic Windows 95 chiselled bevel, matched exactly to the React95 border recipe.
    ///
    /// Every Win95 control is a 2px outer border (top-left light, bottom-right dark) plus a 1px
    /// inner ring (top-left lighter, bottom-right darker). The four documented styles:
    ///   button : TL outer=lightest, TL inner=light,  BR inner=dark,     BR outer=darkest
    ///   window : TL outer=light,    TL inner=lightest, BR inner=dark,    BR outer=darkest
    ///   field  : TL outer=dark,     TL inner=darkest,  BR inner=light,   BR outer=lightest  (sunken)
    ///   thin   : single 1px line (lightest TL / dark BR), or inverted when pressed
    /// Drawn with flat 1px boxes — no masking, no shaders, no rounded corners.
    /// </summary>
    public partial class Win95Bevel : CompositeDrawable
    {
        public enum Style { Button, Window, Field, Thin, ThinPressed, ButtonPressed }

        public Win95Bevel(Style style = Style.Button)
        {
            RelativeSizeAxes = Axes.Both;

            Color4 lightest = Win95.HILIGHT;  // #FEFEFE
            Color4 light = Win95.LIGHT;       // #DFDFDF
            Color4 dark = Win95.SHADOW;       // #848584
            Color4 darkest = Win95.DKSHADOW;  // #0A0A0A

            Color4 tlOuter, tlInner, brInner, brOuter;
            bool hasInner = true;

            switch (style)
            {
                case Style.Window:
                    tlOuter = light; tlInner = lightest; brInner = dark; brOuter = darkest;
                    break;

                case Style.Field:
                    tlOuter = dark; tlInner = darkest; brInner = light; brOuter = lightest;
                    break;

                case Style.ButtonPressed:
                    tlOuter = darkest; tlInner = dark; brInner = light; brOuter = lightest;
                    break;

                case Style.Thin:
                    tlOuter = lightest; brOuter = dark; tlInner = brInner = default; hasInner = false;
                    break;

                case Style.ThinPressed:
                    tlOuter = dark; brOuter = lightest; tlInner = brInner = default; hasInner = false;
                    break;

                default: // Button
                    tlOuter = lightest; tlInner = light; brInner = dark; brOuter = darkest;
                    break;
            }

            var children = new System.Collections.Generic.List<Drawable>
            {
                hEdge(Anchor.TopLeft, tlOuter, 0),
                vEdge(Anchor.TopLeft, tlOuter, 0),
                hEdge(Anchor.BottomLeft, brOuter, 0),
                vEdge(Anchor.TopRight, brOuter, 0),
            };

            if (hasInner)
            {
                children.Add(hEdge(Anchor.TopLeft, tlInner, 1));
                children.Add(vEdge(Anchor.TopLeft, tlInner, 1));
                children.Add(hEdge(Anchor.BottomLeft, brInner, 1));
                children.Add(vEdge(Anchor.TopRight, brInner, 1));
            }

            InternalChildren = children.ToArray();
        }

        // Convenience ctors for the common cases.
        public static Win95Bevel Raised() => new Win95Bevel(Style.Button);
        public static Win95Bevel Sunken() => new Win95Bevel(Style.Field);
        public static Win95Bevel WindowFrame() => new Win95Bevel(Style.Window);

        private static Drawable hEdge(Anchor anchor, Color4 colour, float inset)
        {
            bool top = anchor == Anchor.TopLeft;
            return new Box
            {
                RelativeSizeAxes = Axes.X,
                Height = 1,
                Colour = colour,
                Anchor = anchor,
                Origin = anchor,
                Y = top ? inset : -inset,
                Margin = new MarginPadding { Horizontal = inset },
            };
        }

        private static Drawable vEdge(Anchor anchor, Color4 colour, float inset)
        {
            bool left = anchor == Anchor.TopLeft;
            return new Box
            {
                RelativeSizeAxes = Axes.Y,
                Width = 1,
                Colour = colour,
                Anchor = anchor,
                Origin = anchor,
                X = left ? inset : -inset,
                Margin = new MarginPadding { Vertical = inset },
            };
        }
    }
}
