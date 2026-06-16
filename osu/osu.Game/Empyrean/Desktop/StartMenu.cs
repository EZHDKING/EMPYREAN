// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// The classic Windows 95 Start menu: a raised gray panel with a vertical branded banner on
    /// the left and a list of program entries. Flat, cheap, no animation.
    /// </summary>
    public partial class StartMenu : CompositeDrawable
    {
        public Action OnPlay;
        public Action OnSettings;
        public Action OnSongs;
        public Action OnAbout;
        public Action OnShutDown;

        [BackgroundDependencyLoader]
        private void load()
        {
            Size = new Vector2(220, 300);

            InternalChildren = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                new Win95Bevel(),
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(4),
                    Children = new Drawable[]
                    {
                        // Vertical branded banner (navy with rotated text).
                        new Container
                        {
                            RelativeSizeAxes = Axes.Y,
                            Width = 28,
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.BottomCentre,
                                    Origin = Anchor.BottomCentre,
                                    Rotation = -90,
                                    Y = -8,
                                    Text = "EMPYREAN 95",
                                    Font = OsuFont.GetFont(size: 18, weight: FontWeight.Black),
                                    Colour = Color4.White,
                                },
                            },
                        },
                        // Program entries.
                        new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Padding = new MarginPadding { Left = 34, Top = 2, Right = 2 },
                            Children = new Drawable[]
                            {
                                new StartItem(FontAwesome.Solid.Play, "Play osu!", () => OnPlay?.Invoke()),
                                new StartItem(FontAwesome.Solid.Music, "Beatmaps", () => OnSongs?.Invoke()),
                                new StartItem(FontAwesome.Solid.Cog, "Settings", () => OnSettings?.Invoke()),
                                new StartItem(FontAwesome.Solid.InfoCircle, "About", () => OnAbout?.Invoke()),
                                new Container { RelativeSizeAxes = Axes.X, Height = 8, Child = new Box { RelativeSizeAxes = Axes.X, Height = 1, Anchor = Anchor.Centre, Origin = Anchor.Centre, Colour = Win95.SHADOW } },
                                new StartItem(FontAwesome.Solid.PowerOff, "Shut Down...", () => OnShutDown?.Invoke()),
                            },
                        },
                    },
                },
            };
        }

        private partial class StartItem : Container
        {
            private readonly Action action;
            private readonly Box hover;

            public StartItem(IconUsage icon, string label, Action action)
            {
                this.action = action;
                RelativeSizeAxes = Axes.X;
                Height = 26;

                Children = new Drawable[]
                {
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(8, 0),
                        Margin = new MarginPadding { Left = 4 },
                        Children = new Drawable[]
                        {
                            iconSprite = new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(18), Icon = icon, Colour = Win95.TEXT },
                            label_text = new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = label, Font = OsuFont.GetFont(size: 15), Colour = Win95.TEXT },
                        },
                    },
                };
            }

            private OsuSpriteText label_text;
            private SpriteIcon iconSprite;

            protected override bool OnHover(HoverEvent e)
            {
                hover.Alpha = 1;
                label_text.Colour = Color4.White;
                iconSprite.Colour = Color4.White;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.Alpha = 0;
                label_text.Colour = Win95.TEXT;
                iconSprite.Colour = Win95.TEXT;
            }

            protected override bool OnClick(ClickEvent e)
            {
                action?.Invoke();
                return true;
            }
        }
    }
}
