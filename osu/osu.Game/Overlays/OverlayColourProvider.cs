// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Extensions.Color4Extensions;
using osuTK.Graphics;

namespace osu.Game.Overlays
{
    public class OverlayColourProvider
    {
        /// <summary>
        /// The hue degree associated with the colour shades provided by this <see cref="OverlayColourProvider"/>.
        /// </summary>
        public int Hue { get; private set; }

        public OverlayColourProvider(OverlayColourScheme colourScheme)
            : this(colourScheme.GetHue())
        {
        }

        public OverlayColourProvider(int hue)
        {
            Hue = hue;
        }

        // ===================== EMPYREAN Windows 95 palette override =====================
        // PROJECT/AGENT §5.1 mandates a flat Windows 95 utility look: gray surfaces, navy
        // selection, black text, NO translucency, NO gradients, NO modern hues. The entire
        // osu! overlay system (settings, song-select sidebar, chat, mod select, etc.) pulls
        // its colours from these properties, so remapping them here flips the whole UI to a
        // 95 look in one cheap, central place — exactly the "structural simplification, fewer
        // changes" approach the doctrine prefers over rewriting every panel individually.
        //
        // Win95 system colours:
        //   button face  #C0C0C0   window bg #FFFFFF-ish via light grays
        //   shadow       #808080   dark shadow #000000   highlight #FFFFFF
        //   active title #000080 (navy)   title text #FFFFFF   text #000000
        private static Color4 hex(string h) => Color4Extensions.FromHex(h);

        private static readonly Color4 win95_face = hex("C0C0C0");
        private static readonly Color4 win95_face_light = hex("D4D0C8");
        private static readonly Color4 win95_window = hex("E8E8E8");
        private static readonly Color4 win95_shadow = hex("808080");
        private static readonly Color4 win95_dark = hex("404040");
        private static readonly Color4 win95_navy = hex("000080");
        private static readonly Color4 win95_navy_light = hex("1084D0");
        private static readonly Color4 win95_text = hex("000000");
        private static readonly Color4 win95_white = hex("FFFFFF");

        // Accent / selection colours -> navy (classic Win95 highlight).
        public Color4 Colour0 => win95_navy_light;
        public Color4 Colour1 => win95_navy_light;
        public Color4 Colour2 => win95_navy;
        public Color4 Colour3 => win95_navy;
        public Color4 Colour4 => win95_navy;

        public Color4 Highlight1 => win95_navy;

        // "Content" colours are used for text/icons on top of panels -> black for readability.
        public Color4 Content1 => win95_text;
        public Color4 Content2 => hex("202020");

        // "Light" shades are used for raised/lighter surfaces -> light grays.
        public Color4 Light1 => win95_window;
        public Color4 Light2 => win95_face_light;
        public Color4 Light3 => win95_face;
        public Color4 Light4 => win95_face;

        // "Dark" shades are typically borders/sunken accents -> gray/shadow tones.
        public Color4 Dark1 => win95_shadow;
        public Color4 Dark2 => win95_shadow;
        public Color4 Dark3 => win95_dark;
        public Color4 Dark4 => win95_dark;
        public Color4 Dark5 => win95_dark;
        public Color4 Dark6 => win95_text;

        // Foreground = controls; Background = window chrome. All flat gray (the 95 look).
        public Color4 Foreground1 => win95_face;
        public Color4 Background1 => win95_face_light;
        public Color4 Background2 => win95_face;
        public Color4 Background3 => win95_face;
        public Color4 Background4 => win95_face;
        public Color4 Background5 => win95_shadow;
        public Color4 Background6 => win95_dark;

        /// <summary>
        /// Changes the <see cref="Hue"/> to a different degree.
        /// Note that this does not trigger any kind of signal to any drawable that received colours from here, all drawables need to be updated manually.
        /// </summary>
        /// <param name="colourScheme">The proposed colour scheme.</param>
        public void ChangeColourScheme(OverlayColourScheme colourScheme) => ChangeColourScheme(colourScheme.GetHue());

        /// <summary>
        /// Changes the <see cref="Hue"/> to a different degree.
        /// Note that this does not trigger any kind of signal to any drawable that received colours from here, all drawables need to be updated manually.
        /// </summary>
        /// <param name="hue">The proposed hue degree.</param>
        public void ChangeColourScheme(int hue) => Hue = hue;
    }
}
