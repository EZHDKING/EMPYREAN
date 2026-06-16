// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osu.Game.Overlays.Settings;
using osu.Game.Overlays.Settings.Sections;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Overlays
{
    /// <summary>
    /// The EMPYREAN "Control Panel" — a Windows 95 settings window that replaces the modern osu!
    /// settings overlay on the desktop. It hosts the REAL osu! settings sections (General, Skin,
    /// Input, User Interface, Gameplay, Rulesets, Audio, Graphics, Online, Maintenance, Debug)
    /// inside a Win95 window, picked from a left-hand list like the classic Control Panel.
    ///
    /// We cache an <see cref="OverlayColourProvider"/> (remapped to the flat Win95 palette) so the
    /// hosted sections render in the gray 95 theme rather than the modern dark look.
    /// </summary>
    public partial class EmpyreanControlPanel : OverlayContainer
    {
        private DependencyContainer dependencies;
        private Container sectionContainer;
        private FillFlowContainer sidebar;

        private readonly List<(string label, SettingsSection section)> sections = new List<(string, SettingsSection)>();

        protected override bool BlockNonPositionalInput => true;

        public EmpyreanControlPanel()
        {
            RelativeSizeAxes = Axes.Both;
        }

        protected override IReadOnlyDependencyContainer CreateChildDependencies(IReadOnlyDependencyContainer parent)
        {
            dependencies = new DependencyContainer(base.CreateChildDependencies(parent));
            // Win95-flat colour provider (our OverlayColourProvider override already maps every
            // shade to the gray 95 palette, so the hosted sections come out flat/gray).
            dependencies.CacheAs(new OverlayColourProvider(OverlayColourScheme.Purple));
            return dependencies;
        }

        private bool built;

        [BackgroundDependencyLoader]
        private void load()
        {
            // Keep BDL minimal: only the modal backdrop. The (heavy) real osu! sections and the
            // key-binding panel are built lazily on first open via buildIfNeeded(), so nothing
            // here can break the MainMenu load chain even if a section would throw.
            Add(new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(0, 0, 0, 90) });
        }

        private void buildIfNeeded()
        {
            if (built)
                return;

            built = true;

            // The Input section's "Configure" button opens this key-binding panel; it must be a
            // real instance (passing null would NRE at construction) and must be in the tree.
            var keyBindingPanel = new osu.Game.Overlays.Settings.Sections.Input.KeyBindingPanel();

            // Build the real osu! sections once.
            sections.Add(("General", new GeneralSection()));
            sections.Add(("Skin", new SkinSection()));
            sections.Add(("Input", new InputSection(keyBindingPanel)));
            sections.Add(("User Interface", new UserInterfaceSection()));
            sections.Add(("Gameplay", new GameplaySection()));
            sections.Add(("Rulesets", new RulesetSection()));
            sections.Add(("Audio", new AudioSection()));
            sections.Add(("Graphics", new GraphicsSection()));
            sections.Add(("Online", new OnlineSection()));
            sections.Add(("Maintenance", new MaintenanceSection()));
            sections.Add(("Debug", new DebugSection()));

            var window = new Win95Window("Control Panel — EMPYREAN", FontAwesome.Solid.Cog)
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Size = new Vector2(720, 540),
                OnClose = Hide,
            };

            window.Add(new GridContainer
            {
                RelativeSizeAxes = Axes.Both,
                ColumnDimensions = new[]
                {
                    new Dimension(GridSizeMode.Absolute, 150),
                    new Dimension(),
                },
                Content = new[]
                {
                    new Drawable[]
                    {
                        // Left: section list (sunken white panel).
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Right = 4 },
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                                new Win95Bevel(Win95Bevel.Style.Field),
                                new BasicScrollContainer
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    ScrollbarVisible = false,
                                    Padding = new MarginPadding(3),
                                    Child = sidebar = new FillFlowContainer
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                        Direction = FillDirection.Vertical,
                                    },
                                },
                            },
                        },
                        // Right: the selected section. osu!'s settings controls are built for a
                        // dark theme (light text), so the content area uses a dark Win95-framed
                        // panel — readable, while the window chrome stays Windows 95.
                        new Container
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
                                    Padding = new MarginPadding(6),
                                    Child = sectionContainer = new Container
                                    {
                                        RelativeSizeAxes = Axes.X,
                                        AutoSizeAxes = Axes.Y,
                                    },
                                },
                            },
                        },
                    },
                },
            });

            Add(window);

            // Host the key-binding sub-panel so the Input section's Configure button can show it.
            Add(keyBindingPanel);

            // Populate the sidebar list.
            for (int i = 0; i < sections.Count; i++)
            {
                int index = i;
                sidebar.Add(new SectionRow(sections[i].label, () => showSection(index)));
            }

            showSection(0);
        }

        private void showSection(int index)
        {
            if (index < 0 || index >= sections.Count)
                return;

            sectionContainer.Clear(false); // don't dispose — sections are reused
            sectionContainer.Add(sections[index].section);
        }

        protected override void PopIn()
        {
            buildIfNeeded();
            this.FadeIn(120, Easing.OutQuint);
        }
        protected override void PopOut() => this.FadeOut(120, Easing.OutQuint);

        /// <summary>A Win95 list row in the section sidebar.</summary>
        private partial class SectionRow : Container
        {
            private readonly System.Action onClick;
            private readonly Box hover;
            private readonly OsuSpriteText text;

            public SectionRow(string label, System.Action onClick)
            {
                this.onClick = onClick;
                RelativeSizeAxes = Axes.X;
                Height = 24;

                Children = new Drawable[]
                {
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    text = new OsuSpriteText
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        Margin = new MarginPadding { Left = 6 },
                        Text = label,
                        Font = OsuFont.GetFont(size: 15),
                        Colour = Win95.TEXT,
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                hover.Alpha = 1;
                text.Colour = Color4.White;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.Alpha = 0;
                text.Colour = Win95.TEXT;
            }

            protected override bool OnClick(ClickEvent e)
            {
                onClick?.Invoke();
                return true;
            }
        }
    }
}
