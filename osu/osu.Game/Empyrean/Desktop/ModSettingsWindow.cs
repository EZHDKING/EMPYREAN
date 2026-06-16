// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Game.Configuration;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets.Mods;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Windows 95 popup that exposes a mod's adjustable settings (e.g. Double Time's speed,
    /// Difficulty Adjust's values). It hosts the mod's own generated setting controls via
    /// <see cref="SettingSourceExtensions.CreateSettingsControls"/>, so edits apply to the exact
    /// mod instance that is in the active mod list.
    /// </summary>
    public partial class ModSettingsWindow : Win95Window
    {
        private readonly Mod mod;
        private readonly Action onChanged;

        public ModSettingsWindow(Mod mod, Action onChanged = null)
            : base(titleFor(mod), FontAwesome.Solid.Cog)
        {
            this.mod = mod;
            this.onChanged = onChanged;
            Name = titleFor(mod);
            Size = new Vector2(420, 300);
        }

        private static string titleFor(Mod mod) => $"{mod.Name} settings";

        [osu.Framework.Allocation.BackgroundDependencyLoader]
        private void load()
        {
            var content = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
                Spacing = new Vector2(0, 8),
                Padding = new MarginPadding(10),
            };

            var controls = mod.CreateSettingsControls().ToList();

            if (controls.Count == 0)
                content.Add(new OsuSpriteText { Text = "This mod has no adjustable settings.", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14) });
            else
            {
                foreach (var c in controls)
                    content.Add(c);
            }

            // Re-commit the selection when this popup closes, so gameplay uses the edited values
            // (fixes "DT stays at default" — the live mod instance now propagates on close).
            if (onChanged != null)
            {
                var prev = OnClose;
                OnClose = () =>
                {
                    onChanged();
                    prev?.Invoke();
                };
            }

            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(40, 40, 40, 255) },
                    new Win95Bevel(Win95Bevel.Style.Field),
                    new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        ScrollbarVisible = true,
                        Padding = new MarginPadding(4),
                        Child = content,
                    },
                },
            });
        }
    }
}
