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
using osuTK;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A small Windows 95 "Rename" dialog: a text field plus OK / Cancel.
    /// </summary>
    public partial class RenameWindow : Win95Window
    {
        private readonly string initial;
        private readonly Action<string> onConfirm;
        private BasicTextBox textBox;

        public RenameWindow(string initial, Action<string> onConfirm)
            : base("Rename", FontAwesome.Solid.PencilAlt)
        {
            this.initial = initial;
            this.onConfirm = onConfirm;
            Name = "Rename";
            Size = new Vector2(320, 130);
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
                Padding = new MarginPadding(12),
                Children = new Drawable[]
                {
                    new OsuSpriteText { Text = "New name:", Font = OsuFont.GetFont(size: 15), Colour = Win95.TEXT },
                    textBox = new BasicTextBox
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 26,
                        Text = initial,
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
                            Win95Button.Text("OK", confirm, 70, 24),
                            Win95Button.Text("Cancel", close, 70, 24),
                        },
                    },
                },
            });
        }

        private void confirm()
        {
            onConfirm?.Invoke(textBox?.Text ?? string.Empty);
            close();
        }

        private void close()
        {
            OnClose?.Invoke();
            Expire();
        }
    }
}
