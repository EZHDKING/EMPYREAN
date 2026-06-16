// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Platform;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osuTK;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Windows 95 "Server" dialog. Shows the current osu! server and lets the user point the
    /// client at any host (e.g. dev.ppy.sh or a private server). The host is saved and applied on
    /// the next launch — the API connection is established once at startup, so a restart is needed.
    /// </summary>
    public partial class ServerSwitcherWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private Storage storage { get; set; }

        private BasicTextBox host;
        private OsuSpriteText note;

        public ServerSwitcherWindow()
            : base("Server", FontAwesome.Solid.Globe)
        {
            Name = "Server";
            Size = new Vector2(420, 240);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            string current = api?.Endpoints?.WebsiteUrl ?? "https://dev.ppy.sh";
            string saved = EmpyreanServerStore.LoadHost(storage) ?? current;

            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Padding = new MarginPadding(14),
                Children = new Drawable[]
                {
                    new OsuSpriteText { Text = "Currently connected to:", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                    new OsuSpriteText { Text = current, Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TITLE },
                    new OsuSpriteText { Text = "Connect to server (host or URL):", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT, Margin = new MarginPadding { Top = 6 } },
                    host = new BasicTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 28,
                        Text = saved,
                        PlaceholderText = "dev.ppy.sh",
                    },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Children = new Drawable[]
                        {
                            Win95Button.Text("Save", save, 84, 26),
                            Win95Button.Text("Reset", reset, 84, 26),
                        },
                    },
                    note = new OsuSpriteText
                    {
                        Text = "Tip: changes take effect after you restart EMPYREAN.",
                        Font = OsuFont.GetFont(size: 12),
                        Colour = Win95.TEXT_DISABLED,
                    },
                },
            });
        }

        private void save()
        {
            EmpyreanServerStore.SaveHost(storage, host?.Text ?? string.Empty);
            if (note != null)
                note.Text = "Saved. Restart EMPYREAN to connect to the new server.";
        }

        private void reset()
        {
            EmpyreanServerStore.SaveHost(storage, "dev.ppy.sh");
            if (host != null) host.Text = "dev.ppy.sh";
            if (note != null)
                note.Text = "Reset to dev.ppy.sh. Restart to apply.";
        }
    }
}
