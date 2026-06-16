// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Online.API;
using osuTK;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Windows 95 "Log On to osu!" dialog: username + password fields with Log On / Log Off,
    /// plus a read-only display of the current server endpoint. Uses the real <see cref="IAPIProvider"/>.
    /// </summary>
    public partial class AccountWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        private BasicTextBox username;
        private BasicTextBox password;
        private OsuSpriteText status;

        private readonly IBindable<APIState> apiState = new Bindable<APIState>();

        public AccountWindow()
            : base("Log On to osu!", FontAwesome.Solid.Key)
        {
            Name = "Log On";
            Size = new Vector2(380, 280);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 10),
                Padding = new MarginPadding(14),
                Children = new Drawable[]
                {
                    new OsuSpriteText { Text = "Enter your osu! username and password:", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                    labelled("User name:", username = new BasicTextBox { RelativeSizeAxes = Axes.X, Height = 26 }),
                    labelled("Password:", password = new BasicTextBox { RelativeSizeAxes = Axes.X, Height = 26 }),
                    new FillFlowContainer
                    {
                        Anchor = Anchor.TopRight,
                        Origin = Anchor.TopRight,
                        AutoSizeAxes = Axes.Both,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(6, 0),
                        Children = new Drawable[]
                        {
                            Win95Button.Text("Log On", logOn, 84, 26),
                            Win95Button.Text("Log Off", logOff, 84, 26),
                        },
                    },
                    status = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 13), Colour = Win95.TITLE },
                    new OsuSpriteText
                    {
                        Text = $"Server: {api?.Endpoints?.WebsiteUrl ?? "(default)"}",
                        Font = OsuFont.GetFont(size: 12),
                        Colour = Win95.TEXT_DISABLED,
                    },
                },
            });

            if (api != null)
            {
                apiState.BindTo(api.State);
                apiState.BindValueChanged(s => updateStatus(s.NewValue), true);
            }
        }

        private Drawable labelled(string label, Drawable field) => new FillFlowContainer
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Direction = FillDirection.Vertical,
            Spacing = new Vector2(0, 3),
            Children = new[]
            {
                new OsuSpriteText { Text = label, Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT },
                field,
            },
        };

        private void updateStatus(APIState state)
        {
            if (status == null)
                return;

            status.Text = state switch
            {
                APIState.Online => $"Logged in as {api?.LocalUser?.Value?.Username}.",
                APIState.Connecting => "Connecting…",
                APIState.Failing => "Connection problem — retrying…",
                APIState.RequiresSecondFactorAuth => "Verification required — see the popup to enter your code.",
                _ => "Not logged in.",
            };
        }

        private void logOn()
        {
            if (api == null || string.IsNullOrWhiteSpace(username?.Text) || string.IsNullOrWhiteSpace(password?.Text))
            {
                if (status != null) status.Text = "Please enter a username and password.";
                return;
            }

            api.Login(username.Text, password.Text);
        }

        private void logOff()
        {
            api?.Logout();
        }
    }
}
