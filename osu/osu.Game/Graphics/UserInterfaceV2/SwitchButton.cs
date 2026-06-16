// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Graphics.UserInterfaceV2
{
    /// <summary>
    /// EMPYREAN: a classic Windows 95 square checkbox replacing the modern pill switch.
    ///
    /// A sunken white box with a hard 1px dark border and a black tick when checked. There is
    /// no animation, no rounded corner, no glow — it is drawn from a few flat <see cref="Box"/>es
    /// plus a tick sprite, exactly matching the 95 utility aesthetic the project mandates (§5.1).
    /// The public surface (the <see cref="Checkbox"/> contract, WIDTH, ExpandOnCurrent, PlaySample)
    /// is preserved so every existing call site keeps working unchanged.
    /// </summary>
    public partial class SwitchButton : Checkbox
    {
        // Kept for layout compatibility with call sites that reserve WIDTH for the control.
        public const float WIDTH = 18;

        private const float box_size = 16;

        private readonly Box face;
        private readonly SpriteIcon tick;

        // Win95 system colours (local copies to avoid extra deps).
        private static readonly Color4 win95_white = Color4Extensions.FromHex("FFFFFF");
        private static readonly Color4 win95_shadow = Color4Extensions.FromHex("808080");
        private static readonly Color4 win95_dark = Color4Extensions.FromHex("000000");
        private static readonly Color4 win95_face = Color4Extensions.FromHex("C0C0C0");

        public bool ExpandOnCurrent { get; init; } = true;

        private Sample? sampleChecked;
        private Sample? sampleUnchecked;

        public SwitchButton()
        {
            Size = new Vector2(WIDTH, box_size);

            InternalChild = new Container
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(box_size),
                Masking = false,
                Children = new Drawable[]
                {
                    // Outer dark border (sunken look).
                    new Box { RelativeSizeAxes = Axes.Both, Colour = win95_shadow },
                    // Inner white field, inset 2px to reveal the border.
                    face = new Box
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        RelativeSizeAxes = Axes.Both,
                        Width = (box_size - 4) / box_size,
                        Height = (box_size - 4) / box_size,
                        Colour = win95_white,
                    },
                    tick = new SpriteIcon
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Size = new Vector2(box_size - 6),
                        Icon = FontAwesome.Solid.Check,
                        Colour = win95_dark,
                        Alpha = 0,
                    },
                }
            };
        }

        [BackgroundDependencyLoader(true)]
        private void load(AudioManager audio)
        {
            sampleChecked = audio.Samples.Get(@"UI/check-on");
            sampleUnchecked = audio.Samples.Get(@"UI/check-off");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            Current.BindDisabledChanged(_ => updateState());
            Current.BindValueChanged(_ => updateState(), true);

            FinishTransforms(true);
        }

        protected override bool OnHover(HoverEvent e)
        {
            updateState();
            return base.OnHover(e);
        }

        protected override void OnHoverLost(HoverLostEvent e)
        {
            updateState();
            base.OnHoverLost(e);
        }

        protected override void OnUserChange(bool value)
        {
            base.OnUserChange(value);
            PlaySample(value);
        }

        public void PlaySample(bool value)
        {
            if (value)
                sampleChecked?.Play();
            else
                sampleUnchecked?.Play();
        }

        private void updateState()
        {
            // Hard, instant state — no fades (every frame matters; flat 95 look).
            tick.Alpha = Current.Value ? 1 : 0;

            Color4 field = Current.Disabled ? win95_face : win95_white;
            face.Colour = field;
            tick.Colour = Current.Disabled ? win95_shadow : win95_dark;
        }
    }
}
