// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
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
    /// A classic Windows 95 right-click context menu: a raised gray panel with rows that
    /// highlight navy on hover. Built from flat primitives.
    /// </summary>
    public partial class Win95ContextMenu : CompositeDrawable
    {
        public Win95ContextMenu(IEnumerable<(string label, Action action, bool separatorAfter)> items)
        {
            AutoSizeAxes = Axes.Both;

            var flow = new FillFlowContainer
            {
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Vertical,
                Padding = new MarginPadding(3),
            };

            foreach (var (label, action, separatorAfter) in items)
            {
                flow.Add(new MenuRow(label, () =>
                {
                    // Dismiss the menu first, then run the action (so the action's own windows/
                    // overlays aren't immediately covered or dismissed by lingering menu input).
                    Expire();
                    action?.Invoke();
                }));
                if (separatorAfter)
                    flow.Add(new Container { Width = 150, Height = 7, Child = new Box { RelativeSizeAxes = Axes.X, Height = 1, Anchor = Anchor.Centre, Origin = Anchor.Centre, Colour = Win95.SHADOW } });
            }

            InternalChildren = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                new Win95Bevel(Win95Bevel.Style.Button),
                flow,
            };
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Alpha > 0.01f && base.ReceivePositionalInputAt(screenSpacePos);

        private partial class MenuRow : Container
        {
            private readonly Action action;
            private readonly Box hover;
            private readonly OsuSpriteText text;

            public MenuRow(string label, Action action)
            {
                this.action = action;
                Width = 160;
                Height = 22;

                Children = new Drawable[]
                {
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    text = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 10 },
                        Text = label,
                        Font = OsuFont.GetFont(size: 14),
                        Colour = Win95.TEXT,
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                hover.Alpha = 1;
                text.Colour = Color4.White;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.Alpha = 0;
                text.Colour = Win95.TEXT;
            }

            protected override bool OnClick(ClickEvent e)
            {
                action?.Invoke();
                return true;
            }
        }
    }
}
