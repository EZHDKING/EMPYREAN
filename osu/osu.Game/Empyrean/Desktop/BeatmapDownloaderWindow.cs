// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Beatmaps;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Overlays;
using osu.Game.Overlays.BeatmapListing;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A stripped-down, Windows-95-styled beatmap downloader (osu!direct). Full-text search plus
    /// sort criteria, sort direction and a status/category filter — all cycled via Win95 buttons.
    /// Each result row shows the set, mapper, status and difficulty count, with a Download button.
    /// </summary>
    public partial class BeatmapDownloaderWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved(canBeNull: true)]
        private BeatmapModelDownloader downloader { get; set; }

        private BasicTextBox searchBox;
        private FillFlowContainer list;
        private OsuSpriteText statusText;

        private SortCriteria sort = SortCriteria.Relevance;
        private SortDirection direction = SortDirection.Descending;
        private SearchCategory category = SearchCategory.Any;

        private Win95Button sortButton;
        private Win95Button dirButton;
        private Win95Button catButton;

        public BeatmapDownloaderWindow()
            : base("Get Beatmaps", FontAwesome.Solid.Download)
        {
            Name = "Get Beatmaps";
            Size = new Vector2(620, 500);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },

                    // Search bar.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 38,
                        Padding = new MarginPadding(6),
                        Children = new Drawable[]
                        {
                            searchBox = new BasicTextBox
                            {
                                RelativeSizeAxes = Axes.X,
                                Width = 0.82f,
                                Height = 26,
                                PlaceholderText = "Search… (artist, title, mapper; try \"stars>5\" or \"status=ranked\")",
                                CommitOnFocusLost = false,
                            },
                            Win95Button.Text("Search", doSearch, 90, 24).With(b =>
                            {
                                b.Anchor = Anchor.TopRight;
                                b.Origin = Anchor.TopRight;
                            }),
                        },
                    },

                    // Sort / direction / category filter bar.
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        Margin = new MarginPadding { Top = 40 },
                        Padding = new MarginPadding { Horizontal = 6 },
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Children = new Drawable[]
                        {
                            new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = "Sort:", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT },
                            sortButton = labelButton($"{sort}", 120, cycleSort),
                            dirButton = labelButton(direction == SortDirection.Descending ? "Desc \u25BC" : "Asc \u25B2", 80, cycleDirection),
                            new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = "Status:", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT, Margin = new MarginPadding { Left = 8 } },
                            catButton = labelButton($"{category}", 120, cycleCategory),
                        },
                    },

                    // Result list.
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 76, Bottom = 28, Left = 6, Right = 6 },
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

                    // Status strip.
                    statusText = new OsuSpriteText
                    {
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Margin = new MarginPadding { Left = 8, Bottom = 6 },
                        Font = OsuFont.GetFont(size: 13),
                        Colour = Win95.TEXT,
                        Text = "Type a search and press Enter.",
                    },
                },
            });

            if (searchBox != null)
                searchBox.OnCommit += (_, __) => doSearch();
        }

        private Win95Button labelButton(string text, float width, Action action)
        {
            var b = new Win95Button { Size = new Vector2(width, 24), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft };
            b.Action = action;
            b.Add(new OsuSpriteText { Anchor = Anchor.Centre, Origin = Anchor.Centre, Text = text, Font = OsuFont.GetFont(size: 12), Colour = Win95.TEXT });
            return b;
        }

        private void setButtonText(Win95Button b, string text)
        {
            // Replace the label (the only OsuSpriteText child).
            foreach (var c in b.Children)
            {
                if (c is OsuSpriteText t)
                {
                    t.Text = text;
                    break;
                }
            }
        }

        private void cycleSort()
        {
            // Cycle the most useful osu!direct sort options.
            sort = sort switch
            {
                SortCriteria.Relevance => SortCriteria.Ranked,
                SortCriteria.Ranked => SortCriteria.Rating,
                SortCriteria.Rating => SortCriteria.Plays,
                SortCriteria.Plays => SortCriteria.Favourites,
                SortCriteria.Favourites => SortCriteria.Title,
                SortCriteria.Title => SortCriteria.Artist,
                SortCriteria.Artist => SortCriteria.Difficulty,
                _ => SortCriteria.Relevance,
            };
            setButtonText(sortButton, $"{sort}");
            doSearch();
        }

        private void cycleDirection()
        {
            direction = direction == SortDirection.Descending ? SortDirection.Ascending : SortDirection.Descending;
            setButtonText(dirButton, direction == SortDirection.Descending ? "Desc \u25BC" : "Asc \u25B2");
            doSearch();
        }

        private void cycleCategory()
        {
            category = category switch
            {
                SearchCategory.Any => SearchCategory.Ranked,
                SearchCategory.Ranked => SearchCategory.Qualified,
                SearchCategory.Qualified => SearchCategory.Loved,
                SearchCategory.Loved => SearchCategory.Pending,
                SearchCategory.Pending => SearchCategory.Graveyard,
                _ => SearchCategory.Any,
            };
            setButtonText(catButton, $"{category}");
            doSearch();
        }

        private void doSearch()
        {
            if (api == null || api.State.Value != APIState.Online)
            {
                setStatus("Sign in to search and download beatmaps.");
                return;
            }

            var rs = ruleset?.Value;
            if (rs == null)
                return;

            setStatus("Searching…");
            list.Clear();

            try
            {
                var req = new SearchBeatmapSetsRequest(
                    searchBox?.Text ?? string.Empty,
                    rs,
                    searchCategory: category,
                    sortCriteria: sort,
                    sortDirection: direction);

                req.Success += response => Schedule(() =>
                {
                    try { populate(response); }
                    catch (Exception ex) { setStatus($"Display error: {ex.Message}"); }
                });
                req.Failure += ex => Schedule(() => setStatus($"Search failed: {ex.Message}"));
                api.Queue(req);
            }
            catch (Exception ex)
            {
                setStatus($"Could not search: {ex.Message}");
            }
        }

        private void populate(SearchBeatmapSetsResponse response)
        {
            list.Clear();

            int count = 0;
            if (response?.BeatmapSets != null)
            {
                foreach (var set in response.BeatmapSets)
                    list.Add(new DownloadRow(set, downloader, count++ % 2 == 0));
            }

            setStatus(count == 0 ? "No results." : $"{count} sets found.");
        }

        private void setStatus(string text)
        {
            if (statusText != null)
                statusText.Text = text;
        }

        private partial class DownloadRow : Container
        {
            private readonly APIBeatmapSet set;
            private readonly BeatmapModelDownloader downloader;
            private OsuSpriteText buttonLabel;

            public DownloadRow(APIBeatmapSet set, BeatmapModelDownloader downloader, bool alt)
            {
                this.set = set;
                this.downloader = downloader;
                RelativeSizeAxes = Axes.X;
                Height = 52;

                int diffCount = set.Beatmaps?.Length ?? 0;
                double maxStar = 0;
                if (set.Beatmaps != null)
                {
                    foreach (var b in set.Beatmaps)
                        if (b.StarRating > maxStar) maxStar = b.StarRating;
                }

                var info = new FillFlowContainer
                {
                    Anchor = Anchor.CentreLeft,
                    Origin = Anchor.CentreLeft,
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Margin = new MarginPadding { Left = 6 },
                    Children = new Drawable[]
                    {
                        new OsuSpriteText { Text = $"{set.Artist} - {set.Title}", Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT },
                        new OsuSpriteText { Text = $"mapped by {set.AuthorString}", Font = OsuFont.GetFont(size: 12), Colour = Win95.TEXT_DISABLED },
                        new OsuSpriteText { Text = $"{set.Status}  ·  {diffCount} diff{(diffCount == 1 ? "" : "s")}  ·  up to {maxStar:0.00}\u2605", Font = OsuFont.GetFont(size: 11), Colour = Win95.TITLE },
                    },
                };

                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = alt ? new Color4(238, 238, 238, 255) : Color4.White },
                    info,
                    makeDownloadButton(),
                };
            }

            private Drawable makeDownloadButton()
            {
                var b = new Win95Button { Size = new Vector2(100, 28), Anchor = Anchor.CentreRight, Origin = Anchor.CentreRight, Margin = new MarginPadding { Right = 6 } };
                b.Action = download;
                b.Add(buttonLabel = new OsuSpriteText
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Text = "Download",
                    Font = OsuFont.GetFont(size: 13),
                    Colour = Win95.TEXT,
                });
                return b;
            }

            private void download()
            {
                if (downloader == null)
                {
                    if (buttonLabel != null) buttonLabel.Text = "n/a";
                    return;
                }

                bool started = downloader.Download(set);
                if (buttonLabel != null)
                    buttonLabel.Text = started ? "Downloading" : "Queued";
            }
        }
    }
}
