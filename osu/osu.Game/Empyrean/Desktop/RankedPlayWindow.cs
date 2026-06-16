// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osu.Game.Online.API.Requests;
using osu.Game.Online.API.Requests.Responses;
using osu.Game.Online.Matchmaking;
using osu.Game.Online.Multiplayer;
using osu.Game.Rulesets;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// An AOL-"Connecting to America Online"-styled panel for Ranked Play matchmaking. The big
    /// running-man button signs you on (launches the real matchmaking queue), and a sidebar shows
    /// your stats — ELO/rating (live from the matchmaking lobby when available), plus pp, accuracy
    /// and play count from your profile.
    /// </summary>
    public partial class RankedPlayWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<RulesetInfo> ruleset { get; set; }

        [Resolved(canBeNull: true)]
        private MultiplayerClient multiplayer { get; set; }

        /// <summary>Set by the desktop; launches the real ranked-play matchmaking screen.</summary>
        public Action OnQueue;

        private static readonly Color4 aol_blue = new Color4(0, 51, 153, 255);
        private static readonly Color4 aol_grey = new Color4(214, 211, 206, 255);
        private static readonly Color4 runner_yellow = new Color4(255, 204, 0, 255);

        private OsuSpriteText eloValue;
        private OsuSpriteText ppValue;
        private OsuSpriteText accValue;
        private OsuSpriteText playValue;
        private OsuSpriteText statusLine;

        public RankedPlayWindow()
            : base("Connecting To osu! Ranked", FontAwesome.Solid.Running)
        {
            Name = "Ranked Play";
            Size = new Vector2(560, 360);
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

                    // AOL striped title band.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 40,
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = aol_grey },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "Connecting To osu! Ranked Play\u2026",
                                Font = OsuFont.GetFont(size: 20, weight: FontWeight.Bold),
                                Colour = Win95.TEXT,
                            },
                        },
                    },

                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 48, Bottom = 10, Left = 10, Right = 10 },
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(12, 0),
                        Children = new Drawable[]
                        {
                            // Left: big AOL running-man "Sign On" button.
                            new Container
                            {
                                RelativeSizeAxes = Axes.Y,
                                Width = 250,
                                Children = new Drawable[]
                                {
                                    new SignOnButton(() => OnQueue?.Invoke())
                                    {
                                        RelativeSizeAxes = Axes.Both,
                                    },
                                },
                            },

                            // Right: stats sidebar.
                            new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Children = new Drawable[]
                                {
                                    new Box { RelativeSizeAxes = Axes.Both, Colour = aol_blue },
                                    new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                        Padding = new MarginPadding(12),
                                        Spacing = new Vector2(0, 8),
                                        Children = new Drawable[]
                                        {
                                            new OsuSpriteText { Text = "YOUR RANKED STATS", Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold), Colour = runner_yellow },
                                            stat("ELO / Rating", out eloValue),
                                            stat("Performance", out ppValue),
                                            stat("Accuracy", out accValue),
                                            stat("Play Count", out playValue),
                                            statusLine = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 12), Colour = new Color4(200, 220, 255, 255), Margin = new MarginPadding { Top = 6 } },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            });

            // Live ELO from the matchmaking lobby, if/when it arrives.
            if (multiplayer != null)
                multiplayer.MatchmakingLobbyStatusChanged += onLobbyStatus;

            fetchProfileStats();
        }

        private void onLobbyStatus(MatchmakingLobbyStatus status)
        {
            Schedule(() =>
            {
                if (eloValue != null && status.UserRating != null)
                    eloValue.Text = status.UserRating.Value.ToString("N0");
            });
        }

        private void fetchProfileStats()
        {
            if (api == null || api.State.Value != APIState.Online)
            {
                setStatus("Sign in to load your stats.");
                set(eloValue, "-");
                set(ppValue, "-");
                set(accValue, "-");
                set(playValue, "-");
                return;
            }

            setStatus("Loading stats\u2026");

            var localUser = api.LocalUser.Value;
            var req = new GetUserRequest(localUser?.Id, ruleset?.Value);
            req.Success += u => Schedule(() => applyStats(u));
            req.Failure += ex => Schedule(() => setStatus($"Couldn't load stats: {ex.Message}"));
            api.Queue(req);
        }

        private void applyStats(APIUser u)
        {
            var s = u.Statistics;
            if (s == null)
            {
                setStatus("No stats for this mode.");
                return;
            }

            set(ppValue, $"{(long)Math.Clamp(s.PP ?? 0m, 0m, (decimal)long.MaxValue):N0}pp");
            set(accValue, $"{s.Accuracy:0.00}%");
            set(playValue, s.PlayCount.ToString("N0"));

            // ELO: if matchmaking hasn't pushed a live rating, fall back to global rank as a proxy.
            if (eloValue != null && eloValue.Text == "…")
                eloValue.Text = s.GlobalRank.HasValue ? $"#{s.GlobalRank:N0} (rank)" : "unrated";

            setStatus("Ready. Click the runner to queue!");
        }

        private void set(OsuSpriteText t, string v)
        {
            if (t != null) t.Text = v;
        }

        private void setStatus(string text)
        {
            if (statusLine != null) statusLine.Text = text;
        }

        private Drawable stat(string label, out OsuSpriteText valueText)
        {
            valueText = new OsuSpriteText { Text = "…", Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold), Colour = Color4.White };
            return new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Children = new Drawable[]
                {
                    new OsuSpriteText { Text = label, Font = OsuFont.GetFont(size: 12), Colour = new Color4(180, 205, 255, 255) },
                    valueText,
                },
            };
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            if (multiplayer != null)
                multiplayer.MatchmakingLobbyStatusChanged -= onLobbyStatus;
        }

        /// <summary>The big AOL running-man button.</summary>
        private partial class SignOnButton : Container
        {
            private readonly Action onClick;
            private Box bg;

            public SignOnButton(Action onClick)
            {
                this.onClick = onClick;
            }

            [BackgroundDependencyLoader]
            private void load()
            {
                Children = new Drawable[]
                {
                    bg = new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(224, 224, 240, 255) },
                    new Win95Bevel(Win95Bevel.Style.Button),
                    new FillFlowContainer
                    {
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Vertical,
                        Spacing = new Vector2(0, 8),
                        Children = new Drawable[]
                        {
                            new SpriteIcon
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Size = new Vector2(80),
                                Icon = FontAwesome.Solid.Running,
                                Colour = runner_yellow,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "SIGN ON",
                                Font = OsuFont.GetFont(size: 22, weight: FontWeight.Bold),
                                Colour = aol_blue,
                            },
                            new OsuSpriteText
                            {
                                Anchor = Anchor.Centre,
                                Origin = Anchor.Centre,
                                Text = "Queue Ranked Play",
                                Font = OsuFont.GetFont(size: 13),
                                Colour = Win95.TEXT,
                            },
                        },
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                bg.Colour = new Color4(240, 240, 255, 255);
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                bg.Colour = new Color4(224, 224, 240, 255);
            }

            protected override bool OnClick(ClickEvent e)
            {
                onClick?.Invoke();
                return true;
            }
        }
    }
}
