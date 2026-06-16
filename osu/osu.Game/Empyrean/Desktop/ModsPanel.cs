// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Configuration;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A permanent Windows 95 "Mods" panel docked on the right of the desktop. Lists every mod
    /// for the current ruleset grouped by type; clicking a row toggles it (writes straight to the
    /// global SelectedMods bindable, so the choice carries into gameplay). This replaces the
    /// modern mod-select overlay with an always-available 95 control panel.
    /// </summary>
    public partial class ModsPanel : CompositeDrawable
    {
        public const float WIDTH = 230f;

        [Resolved(canBeNull: true)]
        private Bindable<IReadOnlyList<Mod>> selectedMods { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<RulesetInfo> ruleset { get; set; }

        // Host wires this to open a Win95 settings popup for a mod that has adjustable settings.
        public Action<Mod> OnConfigureMod;

        private FillFlowContainer list;
        private readonly List<ModRow> rows = new List<ModRow>();

        [BackgroundDependencyLoader]
        private void load()
        {
            RelativeSizeAxes = Axes.Y;
            Width = WIDTH;

            InternalChildren = new Drawable[]
            {
                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                new Win95Bevel(Win95Bevel.Style.Window),
                new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding(4),
                    Children = new Drawable[]
                    {
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = 20,
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE },
                                new OsuSpriteText
                                {
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Margin = new MarginPadding { Left = 5 },
                                    Text = "Mods",
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold),
                                    Colour = Win95.TITLE_TEXT,
                                },
                            },
                        },
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = 22 },
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                                new Win95Bevel(Win95Bevel.Style.Field),
                                new BasicScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Padding = new MarginPadding(3),
                                    ScrollbarVisible = true,
                                    Child = list = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                    },
                                },
                            },
                        },
                    },
                },
            };

            buildList();

            if (selectedMods != null)
                selectedMods.BindValueChanged(_ => refreshChecks());
        }

        private void buildList()
        {
            rows.Clear();
            list.Clear();

            var instance = ruleset?.Value?.CreateInstance();
            if (instance == null)
            {
                list.Add(new OsuSpriteText { Text = "  (no ruleset)", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 13), Margin = new MarginPadding(4) });
                return;
            }

            foreach (var group in instance.CreateAllMods().GroupBy(m => m.Type))
            {
                list.Add(new OsuSpriteText
                {
                    Text = group.Key.ToString(),
                    Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold),
                    Colour = Win95.TITLE,
                    Margin = new MarginPadding { Top = 6, Left = 2, Bottom = 2 },
                });

                foreach (var mod in group)
                {
                    if (mod.UserPlayable)
                    {
                        var row = new ModRow(mod, toggle);
                        rows.Add(row);
                        list.Add(row);
                    }
                }
            }

            refreshChecks();
        }

        private void toggle(Mod mod)
        {
            if (selectedMods == null)
                return;

            var current = selectedMods.Value.ToList();
            int idx = current.FindIndex(m => m.Acronym == mod.Acronym);

            if (idx >= 0)
                current.RemoveAt(idx);
            else
            {
                current.RemoveAll(m => mod.IncompatibleMods.Any(t => t.IsInstanceOfType(m)));
                current.Add(mod);
            }

            // Commit the new selection first.
            selectedMods.Value = current;

            // THEN, if we just added a mod with adjustable settings, open the popup bound to the
            // SAME instance that is now in the live selection. Editing it updates the mod in place;
            // we re-commit the bindable when the popup changes a value so consumers pick it up.
            if (idx < 0 && mod.GetSettingsSourceProperties().Any())
                OnConfigureMod?.Invoke(mod);
        }

        /// <summary>Re-commit the current selection (used after a mod's settings are edited in a popup).</summary>
        public void CommitMods()
        {
            if (selectedMods == null)
                return;

            // Reassign a fresh list instance so the bindable fires and consumers re-read settings.
            selectedMods.Value = selectedMods.Value.ToList();
        }

        private void refreshChecks()
        {
            var active = selectedMods?.Value ?? Array.Empty<Mod>();
            foreach (var row in rows)
                row.SetChecked(active.Any(m => m.Acronym == row.Acronym));
        }

        public override bool ReceivePositionalInputAt(Vector2 screenSpacePos) => Alpha > 0.01f && base.ReceivePositionalInputAt(screenSpacePos);

        private partial class ModRow : Container
        {
            public string Acronym { get; }
            private readonly Action<Mod> onToggle;
            private readonly Mod mod;
            private readonly SpriteIcon tick;
            private readonly Box hover;

            public ModRow(Mod mod, Action<Mod> onToggle)
            {
                this.mod = mod;
                this.onToggle = onToggle;
                Acronym = mod.Acronym;
                RelativeSizeAxes = Axes.X;
                Height = 20;

                Children = new Drawable[]
                {
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    new Container
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Size = new Vector2(13),
                        Margin = new MarginPadding { Left = 3 },
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                            new Win95Bevel(Win95Bevel.Style.Field),
                            tick = new SpriteIcon { Anchor = Anchor.Centre, Origin = Anchor.Centre, Size = new Vector2(9), Icon = FontAwesome.Solid.Check, Colour = Win95.TEXT, Alpha = 0 },
                        },
                    },
                    label = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 22 },
                        Text = $"{mod.Acronym} — {mod.Name}",
                        Font = OsuFont.GetFont(size: 13),
                        Colour = Win95.TEXT,
                    },
                };
            }

            private OsuSpriteText label;

            public void SetChecked(bool on) => tick.Alpha = on ? 1 : 0;

            protected override bool OnHover(HoverEvent e)
            {
                hover.Alpha = 1;
                label.Colour = Color4.White;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.Alpha = 0;
                label.Colour = Win95.TEXT;
            }

            protected override bool OnClick(ClickEvent e)
            {
                onToggle?.Invoke(mod);
                return true;
            }
        }
    }
}
