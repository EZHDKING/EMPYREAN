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
    /// A Windows 95 folder window for a "Map Collection". Lists the maps inside the folder;
    /// double-click a map to play it instantly. An "Add maps…" button opens the beatmap browser
    /// so the user can pin more maps into this collection.
    /// </summary>
    public partial class FolderWindow : Win95Window
    {
        private readonly BeatmapShortcut folder;
        private readonly Win95Desktop desktop;
        private FillFlowContainer list;

        public FolderWindow(BeatmapShortcut folder, Win95Desktop desktop)
            : base(folder.Label, FontAwesome.Regular.Folder)
        {
            this.folder = folder;
            this.desktop = desktop;
            Name = folder.Label;
            Size = new Vector2(420, 340);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                    new Win95Bevel(Win95Bevel.Style.Field),
                    new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 3, Left = 3, Right = 3, Bottom = 34 },
                        ScrollbarVisible = true,
                        Child = list = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                        },
                    },
                    // Bottom bar with Add button.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                            Win95Button.Text("Add maps…", () => desktop.OpenBeatmapBrowserForFolder(folder, refresh), 110, 24).With(b =>
                            {
                                b.Anchor = Anchor.CentreLeft;
                                b.Origin = Anchor.CentreLeft;
                                b.Margin = new MarginPadding { Left = 4 };
                            }),
                        },
                    },
                },
            });

            populate();
        }

        /// <summary>Re-read the folder contents (after maps are added).</summary>
        public void refresh() => populate();

        private void populate()
        {
            list.Clear();

            if (folder.Items.Count == 0)
            {
                list.Add(new OsuSpriteText { Text = "  (empty — use \"Add maps…\")", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14), Margin = new MarginPadding(6) });
                return;
            }

            int i = 0;
            foreach (var item in folder.Items)
            {
                var captured = item;
                list.Add(new FolderRow(captured.Label, i++ % 2 == 0, () =>
                {
                    // Close the folder window, then play — so the gameplay transition isn't blocked.
                    OnClose?.Invoke();
                    Expire();
                    desktop.PlayShortcutDirect(captured);
                }));
            }
        }

        private partial class FolderRow : Container
        {
            private readonly Action play;
            private readonly Box hover;
            private readonly OsuSpriteText text;

            public FolderRow(string label, bool alt, Action play)
            {
                this.play = play;
                RelativeSizeAxes = Axes.X;
                Height = 22;

                Children = new Drawable[]
                {
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Margin = new MarginPadding { Left = 4 },
                        Children = new Drawable[]
                        {
                            new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(12), Icon = FontAwesome.Solid.Music, Colour = Win95.TITLE },
                            text = new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = label, Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                        },
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

            protected override bool OnClick(ClickEvent e) => true;

            protected override bool OnDoubleClick(DoubleClickEvent e)
            {
                play?.Invoke();
                return true;
            }
        }
    }
}
