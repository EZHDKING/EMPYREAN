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
using osuTK.Graphics;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// A genuine Windows 95 window: gray raised frame, navy title bar with an icon, title text
    /// and minimize / maximize / close buttons, and a sunken client area. Draggable by the title
    /// bar, focus-to-front on click. This is the core of the EMPYREAN desktop shell — multiple
    /// of these can be open at once (e.g. several beatmap windows) inside the window manager.
    ///
    /// Cheap by construction: flat boxes + bevels, no shadows/blur, only moves while dragged.
    /// </summary>
    public partial class Win95Window : Container
    {
        public const float TITLE_BAR_HEIGHT = 20f;

        public Action OnClose;
        public Action OnActivated;

        private readonly Container client;
        private readonly Box titleBarBg;
        private readonly OsuSpriteText titleText;

        protected override Container<Drawable> Content => client;

        private bool dragging;

        // EMPYREAN: minimized (Alpha 0) windows must not catch input.
        public override bool ReceivePositionalInputAt(osuTK.Vector2 screenSpacePos)
            => Alpha > 0.01f && base.ReceivePositionalInputAt(screenSpacePos);

        public Win95Window(string title, IconUsage? icon = null)
        {
            // Window opens at a default size; callers can override Size/Position.
            Size = new Vector2(520, 360);

            InternalChildren = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                new Win95Bevel(Win95Bevel.Style.Window),
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(4),
                    Children = new Drawable[]
                    {
                        // Title bar
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = TITLE_BAR_HEIGHT,
                            Children = new Drawable[]
                            {
                                titleBarBg = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE },
                                new TitleBarDragArea(this) { RelativeSizeAxes = Axes.Both },
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(4, 0),
                                    Margin = new MarginPadding { Left = 3 },
                                    Children = new Drawable[]
                                    {
                                        new SpriteIcon
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Size = new Vector2(13),
                                            Icon = icon ?? OsuIcon.Logo,
                                            Colour = Color4.White,
                                        },
                                        titleText = new OsuSpriteText
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Text = title,
                                            Colour = Win95.TITLE_TEXT,
                                            Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold),
                                        },
                                    },
                                },
                                // Control buttons (right-aligned).
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    AutoSizeAxes = Axes.Both,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(2, 0),
                                    Margin = new MarginPadding { Right = 2 },
                                    Children = new Drawable[]
                                    {
                                        Win95Button.Icon(FontAwesome.Solid.WindowMinimize, () => Alpha = 0, 16),
                                        Win95Button.Icon(FontAwesome.Regular.Square, () => { }, 16),
                                        Win95Button.Icon(FontAwesome.Solid.Times, close, 16),
                                    },
                                },
                            },
                        },
                        // Client area (sunken).
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = TITLE_BAR_HEIGHT + 2 },
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                                new Win95Bevel(Win95Bevel.Style.Field),
                                client = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(3),
                                },
                            },
                        },
                    },
                },
            };
        }

        public void SetActive(bool active)
        {
            // Active windows have a navy title bar; inactive ones gray (classic Win95).
            titleBarBg.Colour = active ? Win95.TITLE : Win95.SHADOW;
        }

        protected override bool OnMouseDown(MouseDownEvent e)
        {
            OnActivated?.Invoke();
            return base.OnMouseDown(e);
        }

        private void close()
        {
            OnClose?.Invoke();
            Expire();
        }

        // Inner drag handle bound to the parent window so we move the whole window.
        private partial class TitleBarDragArea : Drawable
        {
            private readonly Win95Window window;

            public TitleBarDragArea(Win95Window window)
            {
                this.window = window;
            }

            protected override bool OnDragStart(DragStartEvent e)
            {
                window.dragging = true;
                window.OnActivated?.Invoke();
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                if (window.dragging)
                    window.Position += e.Delta;
            }

            protected override void OnDragEnd(DragEndEvent e)
            {
                window.dragging = false;
            }
        }
    }
}
