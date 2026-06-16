// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// A classic Windows 95 labelled checkbox bound to a <see cref="BindableBool"/>: a sunken
    /// white box with a hard border and a black tick, with the label to the right. No animation.
    /// </summary>
    public partial class Win95CheckRow : Container
    {
        private readonly Bindable<bool> current;
        private readonly SpriteIcon tick;

        public Win95CheckRow(string label, Bindable<bool> current)
        {
            this.current = current;
            RelativeSizeAxes = Axes.X;
            Height = 22;

            Children = new Drawable[]
            {
                new Container
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Size = new Vector2(13),
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                        new Win95Bevel(Win95Bevel.Style.Field),
                        tick = new SpriteIcon
                        {
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Size = new Vector2(9),
                            Icon = FontAwesome.Solid.Check,
                            Colour = Win95.TEXT,
                            Alpha = 0,
                        },
                    },
                },
                new OsuSpriteText
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    Margin = new MarginPadding { Left = 22 },
                    Text = label,
                    Font = OsuFont.GetFont(size: 15),
                    Colour = Win95.TEXT,
                },
            };
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            if (current != null)
                current.BindValueChanged(v => tick.Alpha = v.NewValue ? 1 : 0, true);
        }

        protected override bool OnClick(ClickEvent e)
        {
            if (current != null && !current.Disabled)
                current.Value = !current.Value;
            return true;
        }
    }
}
