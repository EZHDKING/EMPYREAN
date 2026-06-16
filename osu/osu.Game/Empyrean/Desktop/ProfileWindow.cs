// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A stripped-down, Windows-95-styled user profile. Fetches the user from the live API and
    /// shows the core stats (rank, pp, accuracy, play count, grades…) as a plain Win95 properties
    /// sheet — no modern overlay, gradients or covers.
    /// </summary>
    public partial class ProfileWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<RulesetInfo> ruleset { get; set; }

        private readonly long? userId;
        private readonly string username;
        private FillFlowContainer content;

        public ProfileWindow(long? userId, string username)
            : base($"Profile — {username}", FontAwesome.Solid.User)
        {
            this.userId = userId;
            this.username = username;
            Name = "Profile";
            Size = new Vector2(460, 440);
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
                    new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 12, Bottom = 12, Left = 12, Right = 24 },
                        ScrollbarVisible = true,
                        Child = content = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 6),
                        },
                    },
                },
            });

            content.Add(new OsuSpriteText { Text = "Loading profile…", Font = OsuFont.GetFont(size: 15), Colour = Win95.TEXT });
            fetch();
        }

        private void fetch()
        {
            if (api == null || api.State.Value != APIState.Online)
            {
                showOffline();
                return;
            }

            GetUserRequest req = userId != null
                ? new GetUserRequest(userId, ruleset?.Value)
                : new GetUserRequest(username, ruleset?.Value);

            req.Success += u => Schedule(() => populate(u));
            req.Failure += ex => Schedule(() => showError(ex));
            api.Queue(req);
        }

        private void showOffline()
        {
            content.Clear();
            content.Add(header(username));
            content.Add(new OsuSpriteText { Text = "Not signed in — sign in to view full profiles.", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT });
        }

        private void showError(Exception ex)
        {
            content.Clear();
            content.Add(header(username));
            content.Add(new OsuSpriteText { Text = $"Could not load profile: {ex.Message}", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT });
        }

        private void populate(APIUser u)
        {
            content.Clear();

            content.Add(header(u.Username));
            content.Add(new OsuSpriteText { Text = $"Country: {u.CountryCode}", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT });

            var s = u.Statistics;
            if (s == null)
            {
                content.Add(new OsuSpriteText { Text = "(no statistics for this mode)", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT_DISABLED });
                return;
            }

            content.Add(separator());
            content.Add(group("Ranking"));
            content.Add(field("Global Rank", s.GlobalRank.HasValue ? $"#{s.GlobalRank:N0}" : "-"));
            content.Add(field("Country Rank", s.CountryRank.HasValue ? $"#{s.CountryRank:N0}" : "-"));
            content.Add(field("Performance", $"{(long)System.Math.Clamp(s.PP ?? 0m, 0m, (decimal)long.MaxValue):N0}pp"));
            content.Add(field("Accuracy", $"{s.Accuracy:0.00}%"));
            content.Add(field("Level", s.Level.Current.ToString()));

            content.Add(separator());
            content.Add(group("Activity"));
            content.Add(field("Ranked Score", s.RankedScore.ToString("N0")));
            content.Add(field("Total Score", s.TotalScore.ToString("N0")));
            content.Add(field("Play Count", s.PlayCount.ToString("N0")));
            content.Add(field("Total Hits", s.TotalHits.ToString("N0")));
            content.Add(field("Max Combo", $"{s.MaxCombo:N0}x"));
            if (s.PlayTime.HasValue)
                content.Add(field("Play Time", $"{s.PlayTime.Value / 3600}h {(s.PlayTime.Value % 3600) / 60}m"));

            content.Add(separator());
            content.Add(group("Grades"));
            var g = s.GradesCount;
            content.Add(field("SS / SSH", $"{g.SS}  /  {g.SSPlus ?? 0}"));
            content.Add(field("S / SH", $"{g.S}  /  {g.SPlus ?? 0}"));
            content.Add(field("A", $"{g.A}"));

            // Fetch the score lists (top plays, pinned, most played) into their own containers.
            fetchScores(u.Id);
        }

        private void fetchScores(int uid)
        {
            content.Add(separator());
            content.Add(group("Pinned Scores"));
            var pinnedBox = sectionBox();
            content.Add(pinnedBox);
            queueScores(uid, ScoreType.Pinned, pinnedBox);

            content.Add(separator());
            content.Add(group("Best Performance"));
            var bestBox = sectionBox();
            content.Add(bestBox);
            queueScores(uid, ScoreType.Best, bestBox);

            content.Add(separator());
            content.Add(group("Most Played"));
            var mostBox = sectionBox();
            content.Add(mostBox);
            queueMostPlayed(uid, mostBox);
        }

        private FillFlowContainer sectionBox()
        {
            var box = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
            };
            box.Add(loadingText());
            return box;
        }

        private void queueScores(int uid, ScoreType type, FillFlowContainer target)
        {
            try
            {
                var req = new GetUserScoresRequest(uid, type, new PaginationParameters(8), ruleset?.Value);
                req.Success += scores => Schedule(() =>
                {
                    target.Clear();
                    if (scores.Count == 0)
                    {
                        target.Add(plain("   (none)"));
                        return;
                    }

                    foreach (var sc in scores)
                    {
                        var bset = sc.Beatmap?.BeatmapSet;
                        string title = bset != null ? $"{bset.Artist} - {bset.Title} [{sc.Beatmap?.DifficultyName}]" : $"beatmap #{sc.BeatmapID}";
                        string ppStr = sc.PP.HasValue ? $"{(long)System.Math.Clamp(sc.PP.Value, 0d, (double)long.MaxValue)}pp" : "-";
                        target.Add(scoreRow(sc.Rank.ToString(), title, $"{sc.Accuracy * 100:0.00}%", ppStr));
                    }
                });
                req.Failure += _ => Schedule(() => { target.Clear(); target.Add(plain("   (couldn't load)")); });
                api.Queue(req);
            }
            catch { target.Clear(); target.Add(plain("   (couldn't load)")); }
        }

        private void queueMostPlayed(int uid, FillFlowContainer target)
        {
            try
            {
                var req = new GetUserMostPlayedBeatmapsRequest(uid, new PaginationParameters(8));
                req.Success += maps => Schedule(() =>
                {
                    target.Clear();
                    if (maps.Count == 0)
                    {
                        target.Add(plain("   (none)"));
                        return;
                    }

                    foreach (var m in maps)
                    {
                        string title = m.BeatmapSet != null ? $"{m.BeatmapSet.Artist} - {m.BeatmapSet.Title}" : "beatmap";
                        target.Add(scoreRow($"{m.PlayCount}\u00D7", title, "", ""));
                    }
                });
                req.Failure += _ => Schedule(() => { target.Clear(); target.Add(plain("   (couldn't load)")); });
                api.Queue(req);
            }
            catch { target.Clear(); target.Add(plain("   (couldn't load)")); }
        }

        private OsuSpriteText loadingText() => new OsuSpriteText { Text = "   loading…", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT_DISABLED };

        private Drawable plain(string text) => new OsuSpriteText { Text = text, Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT_DISABLED };

        private Drawable scoreRow(string left, string title, string acc, string pp)
        {
            string t = title.Length > 42 ? string.Concat(title.AsSpan(0, 42), "…") : title;
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    new OsuSpriteText { Width = 42, Text = left, Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold), Colour = Win95.TITLE },
                    new OsuSpriteText { Width = 300, Text = t, Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT },
                    new OsuSpriteText { Width = 70, Text = acc, Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT },
                    new OsuSpriteText { Text = pp, Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold), Colour = Win95.TEXT },
                },
            };
        }

        private Drawable header(string name) => new FillFlowContainer
        {
            AutoSizeAxes = Axes.Both,
            Direction = FillDirection.Horizontal,
            Spacing = new Vector2(8, 0),
            Children = new Drawable[]
            {
                new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(28), Icon = FontAwesome.Solid.User, Colour = Win95.TITLE },
                new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = name, Font = OsuFont.GetFont(size: 22, weight: FontWeight.Bold), Colour = Win95.TITLE },
            },
        };

        private Drawable group(string title) => new OsuSpriteText
        {
            Text = title,
            Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
            Colour = Win95.TITLE,
        };

        private Drawable field(string label, string value) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                new OsuSpriteText { Text = label, Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                new OsuSpriteText { Anchor = Anchor.TopRight, Origin = Anchor.TopRight, Text = value, Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT },
            },
        };

        private Drawable separator() => new Box { RelativeSizeAxes = Axes.X, Height = 1, Colour = Win95.SHADOW, Margin = new MarginPadding { Vertical = 2 } };
    }
}
