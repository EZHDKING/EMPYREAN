// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// A classic Windows 95 push button: gray face, raised bevel, that visually presses in
    /// (bevel inverts, content nudges 1px) while held. No animation, no rounded corners.
    /// </summary>
    public partial class Win95Button : Container
    {
        public Action Action;

        private readonly Box face;
        private readonly Win95Bevel raised;
        private readonly Win95Bevel sunken;
        private readonly Container contentNudge;
        private readonly Container content;

        protected override Container<Drawable> Content => content;

        public Win95Button()
        {
            CornerRadius = 0;
            Masking = false;

            InternalChildren = new Drawable[]
            {
                face = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                raised = new Win95Bevel(Win95Bevel.Style.Button),
                sunken = new Win95Bevel(Win95Bevel.Style.ButtonPressed) { Alpha = 0 },
                contentNudge = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Child = content = new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Horizontal = 6 },
                    },
                },
            };
        }

        /// <summary>Convenience for a simple text button.</summary>
        public static Win95Button Text(string label, Action action, float width = 80, float height = 23)
        {
            var b = new Win95Button { Size = new Vector2(width, height), Action = action };
            b.Add(new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = label,
                Colour = Win95.TEXT,
                Font = OsuFont.GetFont(size: 14),
            });
            return b;
        }

        /// <summary>Convenience for a square icon button (title bar controls).</summary>
        public static Win95Button Icon(IconUsage icon, Action action, float size = 16)
        {
            var b = new Win95Button { Size = new Vector2(size, size), Action = action };
            b.content.Padding = new MarginPadding(0);
            b.Add(new SpriteIcon
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(size * 0.55f),
                Icon = icon,
                Colour = Win95.TEXT,
            });
            return b;
        }

        // EMPYREAN: never receive input while invisible (fixes hidden buttons still being clickable).
        public override bool ReceivePositionalInputAt(osuTK.Vector2 screenSpacePos)
            => Alpha > 0.01f && base.ReceivePositionalInputAt(screenSpacePos);

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            if (Alpha <= 0.01f)
                return false;

            raised.Alpha = 0;
            sunken.Alpha = 1;
            contentNudge.Position = new Vector2(1, 1);
            return true;
        }

        protected override void OnMouseUp(MouseUpEvent e)
        {
            raised.Alpha = 1;
            sunken.Alpha = 0;
            contentNudge.Position = Vector2.Zero;
            base.OnMouseUp(e);
        }

        protected override bool OnClick(ClickEvent e)
        {
            Action?.Invoke();
            return true;
        }
    }
}
