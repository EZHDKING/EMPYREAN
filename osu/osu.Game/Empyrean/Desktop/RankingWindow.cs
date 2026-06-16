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
using osu.Game.Database;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Scoring;
using Realms;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Windows 95 text-mode ranking window for a beatmap difficulty. Shows locally stored
    /// scores (rank, accuracy, combo, player, date). Global rankings require an online lookup;
    /// when unavailable we say so plainly.
    /// </summary>
    public partial class RankingWindow : Win95Window
    {
        private readonly string beatmapId;

        [Resolved(canBeNull: true)]
        private RealmAccess realm { get; set; }

        private FillFlowContainer list;

        public RankingWindow(string beatmapId, string title)
            : base($"Ranking — {title}", FontAwesome.Solid.Trophy)
        {
            this.beatmapId = beatmapId;
            Name = "Ranking";
            Size = new Vector2(520, 360);
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
                        Padding = new MarginPadding(6),
                        ScrollbarVisible = true,
                        Child = list = new FillFlowContainer
                        {
                            RelativeSizeAxes = Axes.X,
                            AutoSizeAxes = Axes.Y,
                            Direction = FillDirection.Vertical,
                            Spacing = new Vector2(0, 2),
                        },
                    },
                },
            });

            populate();
        }

        private void populate()
        {
            list.Add(row("LOCAL RANKING", "", "", "", true));

            try
            {
                if (realm != null && Guid.TryParse(beatmapId, out var id))
                {
                    var scores = realm.Run(r =>
                    {
                        // Realm's query provider doesn't support Take()/some operators, so we
                        // materialise the matching scores first, then sort/limit in memory.
                        var matching = r.All<ScoreInfo>()
                                         .Filter("BeatmapInfo.ID == $0", id)
                                         .ToList();

                        return matching
                               .OrderByDescending(s => s.TotalScore)
                               .Take(50)
                               .Select(s => (
                                   name: s.User != null ? s.User.Username : "player",
                                   score: s.TotalScore,
                                   acc: s.Accuracy,
                                   combo: s.MaxCombo,
                                   date: s.Date.LocalDateTime
                               ))
                               .ToList();
                    });

                    if (scores.Count == 0)
                        list.Add(plain("  No local scores yet. Play the map to set one!"));
                    else
                    {
                        int rank = 1;
                        foreach (var s in scores)
                        {
                            list.Add(row(
                                $"#{rank}",
                                s.name,
                                $"{s.score:N0}  ({s.acc:P2})",
                                $"{s.combo}x  ·  {s.date:yyyy-MM-dd}",
                                false));
                            rank++;
                        }
                    }
                }
                else
                    list.Add(plain("  Local scores unavailable."));
            }
            catch (Exception ex)
            {
                list.Add(plain($"  error reading scores: {ex.Message}"));
            }

            list.Add(plain(" "));
            list.Add(row("GLOBAL RANKING", "", "", "", true));
            list.Add(plain("  Global leaderboards are shown in-game on the"));
            list.Add(plain("  online results screen (requires sign-in)."));
        }

        private Drawable plain(string text) =>
            new OsuSpriteText { Text = text, Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT };

        private Drawable row(string col1, string col2, string col3, string col4, bool header)
        {
            var font = OsuFont.GetFont(size: header ? 15 : 14, weight: header ? FontWeight.Bold : FontWeight.Regular);
            var colour = header ? Win95.TITLE : Win95.TEXT;

            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Children = new Drawable[]
                {
                    new OsuSpriteText { Width = 44, Text = col1, Font = font, Colour = colour },
                    new OsuSpriteText { Width = 150, Text = col2, Font = font, Colour = colour },
                    new OsuSpriteText { Width = 170, Text = col3, Font = font, Colour = colour },
                    new OsuSpriteText { Text = col4, Font = font, Colour = colour },
                },
            };
        }
    }
}
