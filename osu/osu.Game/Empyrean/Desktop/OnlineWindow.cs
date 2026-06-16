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
    /// A Win95 "Online" folder listing the osu! online play modes — Ranked Play, Multiplayer,
    /// Playlists and Daily Challenge — each opening the corresponding stock osu! screen.
    /// </summary>
    public partial class OnlineWindow : Win95Window
    {
        private readonly Win95Desktop desktop;

        public OnlineWindow(Win95Desktop desktop)
            : base("Online", FontAwesome.Solid.Globe)
        {
            this.desktop = desktop;
            Name = "Online";
            Size = new Vector2(380, 320);
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
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        AutoSizeAxes = Axes.Y,
                        Direction = FillDirection.Vertical,
                        Padding = new MarginPadding(10),
                        Spacing = new Vector2(0, 6),
                        Children = new Drawable[]
                        {
                            row(FontAwesome.Solid.Trophy, "Ranked Play", "Queue ranked — AOL style", () => { OnClose?.Invoke(); Expire(); desktop.OpenRankedPlay(); }),
                            row(FontAwesome.Solid.Medal, "Server Ranking", "Top players on this server", () => { OnClose?.Invoke(); Expire(); desktop.OpenServerRanking(); }),
                            row(FontAwesome.Solid.Users, "Multiplayer", "Play live with others", () => close(desktop.OnOpenMultiplayer)),
                            row(FontAwesome.Solid.ListUl, "Playlists", "Curated map playlists", () => close(desktop.OnOpenPlaylists)),
                            row(FontAwesome.Solid.CalendarDay, "Daily Challenge", "Today's featured map", () => close(desktop.OnOpenDailyChallenge)),
                        },
                    },
                },
            });
        }

        private void close(Action action)
        {
            OnClose?.Invoke();
            Expire();
            action?.Invoke();
        }

        private Drawable row(IconUsage icon, string title, string subtitle, Action onOpen) => new OnlineRow(icon, title, subtitle, onOpen);

        private partial class OnlineRow : Container
        {
            private readonly Action onOpen;
            private readonly Box hover;

            public OnlineRow(IconUsage icon, string title, string subtitle, Action onOpen)
            {
                this.onOpen = onOpen;
                RelativeSizeAxes = Axes.X;
                Height = 54;

                Children = new Drawable[]
                {
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(10, 0),
                        Margin = new MarginPadding { Left = 8 },
                        Children = new Drawable[]
                        {
                            new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(28), Icon = icon, Colour = Win95.TITLE },
                            new FillFlowContainer
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                AutoSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText { Text = title, Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold), Colour = Win95.TEXT },
                                    new OsuSpriteText { Text = subtitle, Font = OsuFont.GetFont(size: 12), Colour = Win95.TEXT_DISABLED },
                                },
                            },
                        },
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                hover.Alpha = 0.15f;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e) => hover.Alpha = 0;

            protected override bool OnClick(ClickEvent e)
            {
                onOpen?.Invoke();
                return true;
            }
        }
    }
}
