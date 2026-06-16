// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Win95 "explorer"-style window listing the local beatmap library. Double-clicking an
    /// entry presents that beatmap (launching the real osu! selection/gameplay path). Multiple
    /// of these can be open at once to compare/analyse maps — the practical benefit of the
    /// windowed shell the project asked for.
    /// </summary>
    public partial class BeatmapBrowserWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private BeatmapManager beatmaps { get; set; }

        // Host wires this to open the Win95 difficulty picker for a set (double-click).
        public Action<IBeatmapSetInfo> OnOpenSet;
        // Host wires this to pin a map as a desktop shortcut.
        public Action<IBeatmapSetInfo> OnPinToDesktop;

        private FillFlowContainer list;
        private BasicTextBox search;
        private readonly List<(IBeatmapSetInfo set, string label)> allSets = new List<(IBeatmapSetInfo, string)>();

        private enum LocalSort { Title, Artist, Stars, DateAdded }
        private LocalSort sort = LocalSort.Title;
        private Win95Button sortButton;

        private Win95Button makeSortButton()
        {
            var b = new Win95Button { Size = new Vector2(140, 22), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft };
            b.Action = cycleSort;
            b.Add(new OsuSpriteText { Anchor = Anchor.Centre, Origin = Anchor.Centre, Text = $"{sort}", Font = OsuFont.GetFont(size: 12), Colour = Win95.TEXT });
            return b;
        }

        private void cycleSort()
        {
            sort = sort switch
            {
                LocalSort.Title => LocalSort.Artist,
                LocalSort.Artist => LocalSort.Stars,
                LocalSort.Stars => LocalSort.DateAdded,
                _ => LocalSort.Title,
            };

            if (sortButton != null)
                foreach (var c in sortButton.Children)
                    if (c is OsuSpriteText t) { t.Text = $"{sort}"; break; }

            applyFilter();
        }

        public BeatmapBrowserWindow()
            : base("Beatmaps", FontAwesome.Solid.Music)
        {
            Name = "Beatmaps";
            Size = new Vector2(460, 420);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    // Search bar (sunken field) at the top.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 26,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(4, 0),
                                Padding = new MarginPadding(2),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = "Find:", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14) },
                                    search = new BasicTextBox
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        Height = 22,
                                        Width = 0.85f,
                                        PlaceholderText = "type to filter…",
                                    },
                                },
                            },
                        },
                    },
                    // Sort bar.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 26,
                        Margin = new MarginPadding { Top = 28 },
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                            new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Horizontal,
                                Spacing = new Vector2(4, 0),
                                Padding = new MarginPadding(2),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = "Sort:", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 13) },
                                    sortButton = makeSortButton(),
                                },
                            },
                        },
                    },
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 56 },
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                            new Win95Bevel(Win95Bevel.Style.Field),
                            new BasicScrollContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Padding = new MarginPadding(3),
                                ScrollbarVisible = true,
                                Child = list = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.X,
                                    AutoSizeAxes = Axes.Y,
                                    Direction = FillDirection.Vertical,
                                },
                            },
                        },
                    },
                },
            });

            if (search != null)
                search.Current.ValueChanged += _ => applyFilter();

            populate();
        }

        private void applyFilter()
        {
            string q = search?.Current.Value?.Trim().ToLowerInvariant() ?? string.Empty;
            list.Clear();

            // Filter, then sort according to the chosen criteria.
            var filtered = new List<(IBeatmapSetInfo set, string label)>();
            foreach (var entry in allSets)
            {
                if (q.Length == 0 || entry.label.Contains(q, System.StringComparison.OrdinalIgnoreCase))
                    filtered.Add(entry);
            }

            switch (sort)
            {
                case LocalSort.Title:
                    filtered.Sort((a, b) => string.Compare(a.set.Metadata?.Title, b.set.Metadata?.Title, System.StringComparison.OrdinalIgnoreCase));
                    break;

                case LocalSort.Artist:
                    filtered.Sort((a, b) => string.Compare(a.set.Metadata?.Artist, b.set.Metadata?.Artist, System.StringComparison.OrdinalIgnoreCase));
                    break;

                case LocalSort.Stars:
                    filtered.Sort((a, b) => maxStars(b.set).CompareTo(maxStars(a.set)));
                    break;

                case LocalSort.DateAdded:
                    // Keep the store's natural order (most-recently-added first as provided).
                    break;
            }

            int i = 0;
            foreach (var (set, label) in filtered)
            {
                var s = set;
                list.Add(new BeatmapRow(label, i++ % 2 == 0, () => OnOpenSet?.Invoke(s), () => OnPinToDesktop?.Invoke(s)));
            }

            if (list.Children.Count == 0)
                list.Add(new OsuSpriteText { Text = "  no matches.", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14), Margin = new MarginPadding(6) });
        }

        private static double maxStars(IBeatmapSetInfo set)
        {
            double max = 0;
            if (set?.Beatmaps != null)
            {
                foreach (var b in set.Beatmaps)
                    if (b.StarRating > max) max = b.StarRating;
            }
            return max;
        }

        private void populate()
        {
            if (beatmaps == null)
            {
                list.Add(new OsuSpriteText { Text = "  (beatmap library unavailable)", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14), Margin = new MarginPadding(6) });
                return;
            }

            try
            {
                var sets = beatmaps.GetAllUsableBeatmapSets().ToList();

                if (sets.Count == 0)
                {
                    list.Add(new OsuSpriteText { Text = "  No beatmaps installed.", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14), Margin = new MarginPadding(6) });
                    return;
                }

                foreach (var set in sets)
                {
                    var meta = set.Metadata;
                    allSets.Add((set, $"{meta.Artist} - {meta.Title}"));
                }

                applyFilter();
            }
            catch (Exception ex)
            {
                list.Add(new OsuSpriteText { Text = $"  error: {ex.Message}", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 13), Margin = new MarginPadding(6) });
            }
        }

        private partial class BeatmapRow : Container
        {
            private readonly Action present;
            private readonly Action pin;
            private readonly Box selection;
            private readonly OsuSpriteText text;
            private bool selected;

            public BeatmapRow(string label, bool alt, Action present, Action pin)
            {
                this.present = present;
                this.pin = pin;
                RelativeSizeAxes = Axes.X;
                Height = 20;

                Children = new Drawable[]
                {
                    selection = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
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
                            new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(13), Icon = FontAwesome.Solid.Music, Colour = Win95.TITLE },
                            text = new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = label, Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                        },
                    },
                };
            }

            protected override bool OnClick(ClickEvent e)
            {
                selected = !selected;
                selection.Alpha = selected ? 1 : 0;
                text.Colour = selected ? Color4.White : Win95.TEXT;
                return true;
            }

            protected override bool OnDoubleClick(DoubleClickEvent e)
            {
                present?.Invoke();
                return true;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                // Right-click pins the map as a desktop shortcut.
                if (e.Button == osuTK.Input.MouseButton.Right)
                {
                    pin?.Invoke();
                    return true;
                }

                return base.OnMouseDown(e);
            }
        }
    }
}
