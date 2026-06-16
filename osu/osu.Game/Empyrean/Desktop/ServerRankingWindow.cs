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
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// Shows the current server's global Performance ranking in plain IRC/text mode (rank, player,
    /// pp). Uses the live API, so it reflects whichever server EMPYREAN is connected to.
    /// </summary>
    public partial class ServerRankingWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<RulesetInfo> ruleset { get; set; }

        private FillFlowContainer list;

        public ServerRankingWindow()
            : base("Server Ranking", FontAwesome.Solid.Trophy)
        {
            Name = "Server Ranking";
            Size = new Vector2(480, 420);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black },
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
                            Spacing = new Vector2(0, 1),
                        },
                    },
                },
            });

            fetch();
        }

        private void fetch()
        {
            string server = api?.Endpoints?.WebsiteUrl ?? "(server)";
            line($"*** Global Performance Ranking — {server}", new Color4(0, 255, 120, 255));
            line("=================================================", new Color4(0, 160, 90, 255));

            if (api == null || api.State.Value != APIState.Online)
            {
                line("   Not connected. Sign in to view the server ranking.", new Color4(0, 200, 110, 255));
                return;
            }

            var rulesetInfo = ruleset?.Value;
            if (rulesetInfo == null)
            {
                line("   No ruleset selected.", new Color4(0, 200, 110, 255));
                return;
            }

            line("   Loading rankings…", new Color4(0, 200, 110, 255));

            try
            {
                var req = new GetUserRankingsRequest(rulesetInfo);
                req.Success += response => Schedule(() =>
                {
                    try { populate(response); }
                    catch (Exception ex) { line($"   Error displaying rankings: {ex.Message}", new Color4(255, 120, 80, 255)); }
                });
                req.Failure += ex => Schedule(() => line($"   Failed to load rankings: {ex.Message}", new Color4(255, 120, 80, 255)));
                api.Queue(req);
            }
            catch (Exception ex)
            {
                line($"   Could not request rankings: {ex.Message}", new Color4(255, 120, 80, 255));
            }
        }

        private void populate(GetTopUsersResponse response)
        {
            list.Clear();

            string server = api?.Endpoints?.WebsiteUrl ?? "(server)";
            line($"*** Global Performance Ranking — {server}", new Color4(0, 255, 120, 255));
            line("=================================================", new Color4(0, 160, 90, 255));
            line(" RANK   PLAYER                          PERFORMANCE", new Color4(0, 220, 120, 255));
            line("-------------------------------------------------", new Color4(0, 160, 90, 255));

            if (response?.Users == null || response.Users.Count == 0)
            {
                line("   (no ranking data returned)", new Color4(0, 200, 110, 255));
                return;
            }

            int rank = 1;
            foreach (var u in response.Users)
            {
                string name = u?.User?.Username ?? "player";
                if (name.Length > 28) name = name.Substring(0, 28);

                string rankStr = (u.GlobalRank?.ToString() ?? rank.ToString()).PadLeft(5);
                string pp = formatPp(u.PP) + "pp";

                line($" #{rankStr}  {name.PadRight(30)} {pp}", new Color4(0, 255, 120, 255));
                rank++;
            }

            line("", new Color4(0, 160, 90, 255));
            line($"*** {response.Users.Count} players shown.", new Color4(0, 220, 120, 255));
        }

        /// <summary>
        /// Format a pp value safely. PP is a decimal that, on some private servers, can be far
        /// larger than Int32 (e.g. 130 billion), so we widen to long (64-bit) and clamp anything
        /// beyond long.MaxValue rather than overflow.
        /// </summary>
        private static string formatPp(decimal? value)
        {
            if (value == null)
                return "0";

            decimal v = value.Value;
            if (v < 0) v = 0;

            if (v > long.MaxValue)
                return long.MaxValue.ToString("N0") + "+";

            return ((long)v).ToString("N0");
        }

        private void line(string text, Color4 colour)
        {
            list.Add(new OsuSpriteText
            {
                Text = text,
                Font = OsuFont.GetFont(size: 13, weight: FontWeight.Regular),
                Colour = colour,
            });
        }
    }
}
