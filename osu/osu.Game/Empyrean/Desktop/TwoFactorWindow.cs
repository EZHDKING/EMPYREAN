// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
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
    /// A Windows 95 prompt for entering the osu! two-factor (2FA) verification code. Opens
    /// automatically when the API reports it requires a second factor, since otherwise the user
    /// has no on-screen way to provide the code on the Win95 desktop.
    /// </summary>
    public partial class TwoFactorWindow : Win95Window
    {
        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        private BasicTextBox codeBox;
        private OsuSpriteText status;

        public TwoFactorWindow()
            : base("Verification Required", FontAwesome.Solid.Key)
        {
            Name = "Verification";
            Size = new Vector2(380, 210);
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
                    new OsuSpriteText { Text = "A verification code was sent to your email.", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT, AllowMultiline = true },
                    new OsuSpriteText { Text = "Enter the code to finish signing in:", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                    codeBox = new BasicTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 30,
                        PlaceholderText = "00000000",
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
                            Win95Button.Text("Verify", verify, 90, 26),
                            Win95Button.Text("Cancel", cancel, 90, 26),
                        },
                    },
                    status = new OsuSpriteText { Text = "", Font = OsuFont.GetFont(size: 13), Colour = Win95.TITLE },
                },
            });
        }

        private void verify()
        {
            string code = codeBox?.Text?.Trim() ?? string.Empty;
            if (string.IsNullOrEmpty(code))
            {
                if (status != null) status.Text = "Please enter the code.";
                return;
            }

            try
            {
                api?.AuthenticateSecondFactor(code);
                if (status != null) status.Text = "Verifying…";
            }
            catch (Exception ex)
            {
                if (status != null) status.Text = $"Error: {ex.Message}";
            }
        }

        private void cancel()
        {
            OnClose?.Invoke();
            Expire();
        }
    }
}
