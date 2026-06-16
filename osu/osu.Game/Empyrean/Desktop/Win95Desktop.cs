// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Extensions.Color4Extensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Framework.Threading;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// The EMPYREAN Windows 95 desktop shell: a teal desktop, a window-manager layer that hosts
    /// draggable <see cref="Win95Window"/>s (so multiple beatmap/tool windows can be open at
    /// once), and a taskbar with a Start button, per-window buttons and a clock.
    ///
    /// This is an additive shell drawn over the menu — it does not touch the gameplay path. It
    /// is built entirely from flat boxes + bevels (no shadows, blur, or animated gradients) per
    /// the Windows 95 mandate (AGENT §5.1).
    /// </summary>
    public partial class Win95Desktop : Container
    {
        public const float TASKBAR_HEIGHT = 28f;

        private Container windowLayer;
        private FillFlowContainer taskbarButtons;
        private OsuSpriteText clock;
        private StartMenu startMenu;
        private Container iconArea;
        private Container contextMenuLayer;
        private ModsPanel modsPanel;

        private readonly List<Win95Window> windows = new List<Win95Window>();
        private readonly Dictionary<Win95Window, Win95Button> taskButtons = new Dictionary<Win95Window, Win95Button>();

        /// <summary>Invoked when the user picks "Play osu!" — host wires this to launch gameplay.</summary>
        public Action OnLaunchPlay;
        public Action OnOpenSettings;
        public Action OnExit;
        // EMPYREAN: play a specific difficulty straight into gameplay (no song select).
        public Action<osu.Game.Beatmaps.BeatmapInfo> OnPlayBeatmap;

        [osu.Framework.Allocation.Resolved(canBeNull: true)]
        private osu.Framework.Platform.Storage storage { get; set; }

        [osu.Framework.Allocation.Resolved(canBeNull: true)]
        private osu.Game.Beatmaps.BeatmapManager beatmaps { get; set; }

        [osu.Framework.Allocation.Resolved(canBeNull: true)]
        private osu.Game.Online.API.IAPIProvider api { get; set; }

        [osu.Framework.Allocation.Resolved(canBeNull: true)]
        private osu.Game.Online.LocalUserStatisticsProvider statisticsProvider { get; set; }

        [osu.Framework.Allocation.Resolved(canBeNull: true)]
        private osu.Framework.Bindables.Bindable<osu.Game.Rulesets.RulesetInfo> rulesetBindable { get; set; }

        [osu.Framework.Allocation.Resolved(canBeNull: true)]
        private osu.Game.Rulesets.RulesetStore rulesetStore { get; set; }

        // Host wires this to open the existing osu! beatmap listing/downloader overlay.
        public System.Action OnOpenBeatmapDownloader;
        // EMPYREAN: open the song-select editor / skin editor (the stock osu! tools).
        public System.Action OnOpenEditor;
        public System.Action OnOpenSkinEditor;
        // EMPYREAN: open the logged-in user's profile (wired to OsuGame.ShowUser).
        public System.Action OnOpenProfile;
        // EMPYREAN: online play screens (wired to MainMenu push paths).
        public System.Action OnOpenMultiplayer;
        public System.Action OnOpenPlaylists;
        public System.Action OnOpenDailyChallenge;
        public System.Action OnOpenRankedPlay;

        private OsuSpriteText userInfo;
        private Box desktopBackground;
        private Sprite wallpaperSprite;

        private int wallpaperIndex;
        // Bundled wallpaper texture names (null = use the solid colour instead).
        private static readonly (string image, string hex)[] wallpapers =
        {
            ("win95", "008080"),
            ("bliss", "3A6EA5"),
            ("win98", "3A6EA5"),
            ("win98b", "3A6EA5"),
            ("xpgreen", "5A7E3A"),
            ("dunes", "8B3A20"),
            (null, "008080"), // plain teal
            (null, "000000"), // black
        };

        private DesktopShortcutStore shortcutStore;
        private readonly osu.Framework.Bindables.IBindable<osu.Game.Online.API.APIState> apiStateBindable = new osu.Framework.Bindables.Bindable<osu.Game.Online.API.APIState>();
        private bool twoFactorOpen;
        private DesktopState desktopState = new DesktopState();
        private float iconSize => desktopState?.IconSize ?? 32;
        private const float taskbar_reserved = 8f;

        [BackgroundDependencyLoader]
        private void load(osu.Framework.Platform.GameHost host, osu.Framework.Graphics.Rendering.IRenderer renderer)
        {
            RelativeSizeAxes = Axes.Both;

            // Load the embedded Win95/98/2000 icon + wallpaper textures (safe if assets missing).
            osu.Game.Empyrean.UI.EmpyreanAssets.Init(host, renderer);

            InternalChildren = new Drawable[]
            {
                // Teal Win95 desktop background — also the right-click context-menu surface.
                new DesktopSurface(this)
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        desktopBackground = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.WORKSPACE },
                        wallpaperSprite = new Sprite
                        {
                            RelativeSizeAxes = Axes.Both,
                            FillMode = FillMode.Fill,
                            Anchor = Anchor.Centre,
                            Origin = Anchor.Centre,
                            Alpha = 0,
                        },
                    },
                },

                // Desktop icons (shortcuts) — freeform, draggable, grid-arranged.
                iconArea = new PassThroughContainer
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Top = 8, Left = 8, Bottom = TASKBAR_HEIGHT + 8 },
                },

                // Permanent Mods panel docked on the right.
                modsPanel = new ModsPanel
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Margin = new MarginPadding { Bottom = TASKBAR_HEIGHT },
                    OnConfigureMod = mod => OpenWindow(new ModSettingsWindow(mod, () => modsPanel?.CommitMods())),
                },

                // Window manager workspace (above desktop, below taskbar).
                windowLayer = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Padding = new MarginPadding { Bottom = TASKBAR_HEIGHT, Right = ModsPanel.WIDTH },
                },

                // Context menu layer (above windows).
                contextMenuLayer = new Container { RelativeSizeAxes = Axes.Both },

                // Start menu (hidden until toggled).
                startMenu = new StartMenu
                {
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Y = -TASKBAR_HEIGHT,
                    Alpha = 0,
                    OnPlay = () => { hideStart(); OnLaunchPlay?.Invoke(); },
                    OnSettings = () => { hideStart(); OnOpenSettings?.Invoke(); },
                    OnSongs = () => { hideStart(); OpenBeatmapBrowser(); },
                    OnAbout = () => { hideStart(); OpenAbout(); },
                    OnShutDown = () => { hideStart(); OnExit?.Invoke(); },
                },

                // Taskbar.
                new Container
                {
                    RelativeSizeAxes = Axes.X,
                    Height = TASKBAR_HEIGHT,
                    Anchor = Anchor.BottomLeft,
                    Origin = Anchor.BottomLeft,
                    Children = new Drawable[]
                    {
                        new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                        new Win95Bevel(Win95Bevel.Style.Thin),
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Vertical = 3, Horizontal = 2 },
                            Children = new Drawable[]
                            {
                                startButton(),
                                gamemodeSelector(),
                                taskbarButtons = new FillFlowContainer
                                {
                                    RelativeSizeAxes = Axes.Y,
                                    AutoSizeAxes = Axes.X,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(3, 0),
                                    Margin = new MarginPadding { Left = 60 },
                                },
                                // System tray: Get Beatmaps button + user info + clock.
                                new FillFlowContainer
                                {
                                    Anchor = Anchor.CentreRight,
                                    Origin = Anchor.CentreRight,
                                    AutoSizeAxes = Axes.X,
                                    RelativeSizeAxes = Axes.Y,
                                    Direction = FillDirection.Horizontal,
                                    Spacing = new Vector2(4, 0),
                                    Children = new Drawable[]
                                    {
                                        trayButton(FontAwesome.Solid.Download, "Get Beatmaps", OpenBeatmapDownloader),
                                        // User info (clickable) — username · pp · rank. Opens profile.
                                        userInfoButton().With(b =>
                                        {
                                            b.Anchor = Anchor.CentreLeft;
                                            b.Origin = Anchor.CentreLeft;
                                        }),
                                        // Clock (sunken).
                                        new Container
                                        {
                                            Anchor = Anchor.CentreLeft,
                                            Origin = Anchor.CentreLeft,
                                            Size = new Vector2(64, TASKBAR_HEIGHT - 8),
                                            Children = new Drawable[]
                                            {
                                                new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },
                                                new Win95Bevel(Win95Bevel.Style.Field),
                                                clock = new OsuSpriteText
                                                {
                                                    Anchor = Anchor.Centre,
                                                    Origin = Anchor.Centre,
                                                    Font = OsuFont.GetFont(size: 13),
                                                    Colour = Win95.TEXT,
                                                },
                                            },
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };
        }

        private Drawable trayButton(IconUsage icon, string label, System.Action action)
        {
            var b = new Win95Button { Size = new Vector2(108, TASKBAR_HEIGHT - 8), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft };
            b.Action = action;
            b.Add(new FillFlowContainer
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(4, 0),
                Children = new Drawable[]
                {
                    new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(12), Icon = icon, Colour = Win95.TEXT },
                    new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = label, Font = OsuFont.GetFont(size: 12), Colour = Win95.TEXT },
                },
            });
            return b;
        }

        private Win95Button userInfoButton()
        {
            var b = new Win95Button { Size = new Vector2(190, TASKBAR_HEIGHT - 8) };
            b.Action = () =>
            {
                var u = api?.LocalUser?.Value;
                if (u != null && u.Id > 1)
                    OpenProfile(u.Id, u.Username);
            };
            b.Add(userInfo = new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Font = OsuFont.GetFont(size: 12),
                Colour = Win95.TEXT,
                Text = "guest",
            });
            return b;
        }

        private Drawable gamemodeSelector()
        {
            var flow = new FillFlowContainer
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.X,
                RelativeSizeAxes = Axes.Y,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(2, 0),
                Margin = new MarginPadding { Left = 4 },
                Children = new Drawable[]
                {
                    modeButton("osu", "std"),
                    modeButton("taiko", "taiko"),
                    modeButton("fruits", "ctb"),
                    modeButton("mania", "mania"),
                },
            };
            return flow;
        }

        private Drawable modeButton(string shortName, string label)
        {
            var b = new Win95Button { Size = new Vector2(46, TASKBAR_HEIGHT - 8), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft };
            b.Action = () => setRuleset(shortName);
            b.Add(new OsuSpriteText
            {
                Anchor = Anchor.Centre,
                Origin = Anchor.Centre,
                Text = label,
                Font = OsuFont.GetFont(size: 12),
                Colour = Win95.TEXT,
            });
            return b;
        }

        private void setRuleset(string shortName)
        {
            try
            {
                var rs = rulesetStore?.GetRuleset(shortName);
                if (rs != null && rulesetBindable != null)
                    rulesetBindable.Value = rs;
            }
            catch { }
        }

        private Drawable startButton()
        {
            var b = new Win95Button { Size = new Vector2(56, TASKBAR_HEIGHT - 8), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft };
            b.Action = toggleStart;
            b.Add(new FillFlowContainer
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                AutoSizeAxes = Axes.Both,
                Direction = FillDirection.Horizontal,
                Spacing = new Vector2(3, 0),
                Children = new Drawable[]
                {
                    new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(14), Icon = OsuIcon.Logo, Colour = Win95.VW_MAGENTA },
                    new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = "Start", Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT },
                },
            });
            return b;
        }

        private void toggleStart()
        {
            if (startMenu.Alpha > 0.5f) hideStart();
            else startMenu.Alpha = 1;
        }

        private void hideStart() => startMenu.Alpha = 0;

        // ---- right-click desktop context menu --------------------------------------------
        public void ShowDesktopContextMenu(Vector2 position)
        {
            contextMenuLayer.Clear();

            var menu = new Win95ContextMenu(new (string, Action, bool)[]
            {
                ("Open Beatmaps", OpenBeatmapBrowser, false),
                ("New Collection (folder)", CreateFolder, true),
                ("Arrange Icons", ArrangeIcons, false),
                ("Icons: Small", () => SetIconSize(24), false),
                ("Icons: Medium", () => SetIconSize(32), false),
                ("Icons: Large", () => SetIconSize(48), true),
                ("Change Wallpaper", cycleWallpaper, false),
                ("Log On to osu!...", OpenAccount, false),
                ("Refresh", rebuildIcons, true),
                ("Settings", () => OnOpenSettings?.Invoke(), false),
                ("Properties", OpenAbout, false),
            })
            {
                Position = position,
            };

            contextMenuLayer.Add(menu);
        }

        public void HideContextMenu() => contextMenuLayer.Clear();

        private void cycleWallpaper()
        {
            HideContextMenu();
            wallpaperIndex = (wallpaperIndex + 1) % wallpapers.Length;
            applyWallpaper();
        }

        private void applyWallpaper()
        {
            var (image, hex) = wallpapers[wallpaperIndex];

            if (desktopBackground != null)
                desktopBackground.Colour = Color4Extensions.FromHex(hex);

            if (wallpaperSprite == null)
                return;

            var tex = image != null ? osu.Game.Empyrean.UI.EmpyreanAssets.GetWallpaper(image) : null;
            if (tex != null)
            {
                wallpaperSprite.Texture = tex;
                wallpaperSprite.Alpha = 1;
            }
            else
                wallpaperSprite.Alpha = 0;
        }

        // ---- desktop icon system (freeform, draggable, grid-arranged, persisted) --------

        // Layout constants for grid snapping/arranging.
        private float cellW => iconSize + 52;
        private float cellH => iconSize + 46;

        /// <summary>Pin a whole set: opens the difficulty picker (kept for the browser's right-click).</summary>
        public void AddBeatmapShortcut(osu.Game.Beatmaps.IBeatmapSetInfo set)
        {
            // Pinning a set creates a persistent shortcut whose double-click opens the picker.
            if (set == null) return;
            var first = System.Linq.Enumerable.FirstOrDefault(set.Beatmaps);
            if (first is osu.Game.Beatmaps.BeatmapInfo bi)
                CreateDifficultyShortcut(bi);
        }

        /// <summary>Create a PERSISTENT desktop shortcut to a specific difficulty (double-click plays it).</summary>
        public void CreateDifficultyShortcut(osu.Game.Beatmaps.BeatmapInfo beatmap)
        {
            if (beatmap == null)
                return;

            string label = $"{beatmap.Metadata?.Title ?? "map"} [{beatmap.DifficultyName}]";
            var sc = new BeatmapShortcut { Label = label, BeatmapId = beatmap.ID.ToString() };

            placeAtFreeCell(sc);
            desktopState.Items.Add(sc);
            save();
            rebuildIcons();
        }

        /// <summary>Create an empty folder ("Map Collection") on the desktop.</summary>
        public void CreateFolder()
        {
            var sc = new BeatmapShortcut { Label = "New Collection", IsFolder = true };
            placeAtFreeCell(sc);
            desktopState.Items.Add(sc);
            save();
            rebuildIcons();
        }

        private void save() => shortcutStore?.Save(desktopState);

        // ---- icon construction -------------------------------------------------------

        private void rebuildIcons()
        {
            if (iconArea == null) return;
            iconArea.Clear();

            // Built-in program icons first (fixed identities, draggable but not persisted-as-items).
            addProgramIcon(FontAwesome.Solid.Play, "Play osu!", 0, () => OnLaunchPlay?.Invoke(), "play");
            addProgramIcon(FontAwesome.Solid.Music, "Beatmaps", 1, OpenBeatmapBrowser, "beatmaps");
            addProgramIcon(FontAwesome.Solid.Cog, "Settings", 2, () => OnOpenSettings?.Invoke(), "settings");
            addProgramIcon(FontAwesome.Solid.PencilAlt, "Editor", 3, () => OnOpenEditor?.Invoke(), "editor");
            addProgramIcon(FontAwesome.Solid.PaintBrush, "Skin Editor", 4, () => OnOpenSkinEditor?.Invoke(), "skin");
            addProgramIcon(FontAwesome.Solid.Music, "WinAmp", 5, OpenWinamp, "winamp");
            addProgramIcon(FontAwesome.Solid.Globe, "Server", 6, OpenServerSwitcher, "globe");
            addProgramIcon(FontAwesome.Solid.Key, "Log On", 7, OpenAccount, "key");
            addProgramIcon(FontAwesome.Solid.Globe, "Online", 8, OpenOnline, "network");
            addProgramIcon(FontAwesome.Solid.Comments, "AOL Messenger", 9, OpenChat, "user");
            addProgramIcon(FontAwesome.Solid.InfoCircle, "About", 10, OpenAbout, "about");
            addProgramIcon(FontAwesome.Solid.Trophy, "Benchmark", 11, OpenBenchmark, "trophy");
            addProgramIcon(FontAwesome.Solid.Cog, "EZHD Upscaler", 12, OpenPerformance, "settings");
            addProgramIcon(FontAwesome.Solid.Trophy, "FPS Settings", 13, OpenFpsSettings, "trophy");

            // User items (maps + folders) at their saved positions.
            foreach (var sc in desktopState.Items)
                addItemIcon(sc);
        }

        // Program icons live in the left column by default index.
        private void addProgramIcon(IconUsage icon, string label, int slot, System.Action open, string iconName = null)
        {
            // Wrap into columns so icons never run off the bottom of the screen. Fall back to a
            // sensible per-column count if the desktop hasn't been sized yet during first layout.
            int perCol = Math.Max(1, (int)((DrawHeight - TASKBAR_HEIGHT - 24) / cellH));
            if (perCol <= 1 || DrawHeight <= 0)
                perCol = 8;

            int col = slot / perCol;
            int row = slot % perCol;

            var di = new DesktopIcon(icon, label, open, iconSize, iconName)
            {
                Position = new Vector2(col * cellW, row * cellH),
            };
            di.OnMoved = () => snapIcon(di);
            iconArea.Add(di);
        }

        private void addItemIcon(BeatmapShortcut sc)
        {
            IconUsage icon = sc.IsFolder ? FontAwesome.Regular.Folder : FontAwesome.Solid.Play;
            string iconName = sc.IsFolder ? "folder" : "map";

            var di = new DesktopIcon(icon, sc.Label, () => openItem(sc), iconSize, iconName)
            {
                Shortcut = sc,
                IsFolder = sc.IsFolder,
            };

            // Restore saved position, or auto-place if unplaced.
            if (sc.X < 0 || sc.Y < 0)
            {
                placeAtFreeCell(sc);
                save();
            }
            di.Position = new Vector2(sc.X, sc.Y);

            di.OnContextMenu = pos => showIconContextMenu(sc, di, pos);
            di.OnClicked = (icon2, additive) => handleIconClicked(icon2, additive);
            di.OnDropped = (icon2, screenCentre) => handleIconDropped(icon2, screenCentre);
            di.OnMoved = () =>
            {
                snapIcon(di);
                sc.X = di.Position.X;
                sc.Y = di.Position.Y;
                save();
            };

            iconArea.Add(di);
        }

        // Multi-select: a plain click selects only this icon; ctrl/shift-click toggles it in the
        // current selection (so several maps can be dragged into a collection at once).
        // Clear selection on every desktop icon.
        private void clearIconSelection()
        {
            foreach (var c in iconArea.Children.OfType<DesktopIcon>())
                c.setSelected(false);
        }

        // Select all (non-folder) icons whose centre falls within the screen-space rectangle.
        private void selectIconsInRect(Vector2 screenTopLeft, Vector2 screenBottomRight)
        {
            float minX = Math.Min(screenTopLeft.X, screenBottomRight.X);
            float maxX = Math.Max(screenTopLeft.X, screenBottomRight.X);
            float minY = Math.Min(screenTopLeft.Y, screenBottomRight.Y);
            float maxY = Math.Max(screenTopLeft.Y, screenBottomRight.Y);

            foreach (var c in iconArea.Children.OfType<DesktopIcon>())
            {
                var centre = c.ScreenSpaceDrawQuad.Centre;
                bool inside = centre.X >= minX && centre.X <= maxX && centre.Y >= minY && centre.Y <= maxY;
                c.setSelected(inside);
            }
        }

        // Enter starts the currently-selected map icon (mirrors double-click), so users can
        // select a map and just press Enter to play.
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == osuTK.Input.Key.Enter || e.Key == osuTK.Input.Key.KeypadEnter)
            {
                var selectedIcon = iconArea.Children.OfType<DesktopIcon>().FirstOrDefault(c => c.IsSelected && c.Shortcut != null);
                if (selectedIcon != null)
                {
                    openItem(selectedIcon.Shortcut);
                    return true;
                }
            }

            return base.OnKeyDown(e);
        }

        private void handleIconClicked(DesktopIcon icon, bool additive)
        {
            if (!additive)
            {
                foreach (var c in iconArea.Children.OfType<DesktopIcon>())
                    c.setSelected(c == icon);
            }
            else
                icon.setSelected(!icon.IsSelected);
        }

        // Drop handling: if a (non-folder) map icon is released over a folder icon, move every
        // selected map shortcut into that collection, remove them from the desktop, and persist.
        private void handleIconDropped(DesktopIcon dropped, Vector2 screenCentre)
        {
            if (dropped.IsFolder || dropped.Shortcut == null)
                return;

            // Find a folder icon under the drop point.
            DesktopIcon targetFolder = null;
            foreach (var c in iconArea.Children.OfType<DesktopIcon>())
            {
                if (!c.IsFolder || c == dropped)
                    continue;

                if (c.ScreenSpaceDrawQuad.Contains(screenCentre))
                {
                    targetFolder = c;
                    break;
                }
            }

            if (targetFolder?.Shortcut == null)
                return;

            // Gather all selected map shortcuts (plus the dropped one), de-duplicated.
            var toMove = iconArea.Children.OfType<DesktopIcon>()
                                 .Where(c => !c.IsFolder && c.Shortcut != null && (c.IsSelected || c == dropped))
                                 .Select(c => c.Shortcut)
                                 .Distinct()
                                 .ToList();

            if (toMove.Count == 0)
                toMove.Add(dropped.Shortcut);

            foreach (var sc in toMove)
            {
                // Move from the desktop into the folder.
                desktopState.Items.Remove(sc);
                if (!targetFolder.Shortcut.Items.Any(i => i.BeatmapId == sc.BeatmapId && i.Label == sc.Label))
                    targetFolder.Shortcut.Items.Add(sc);
            }

            save();
            rebuildIcons();
        }

        private void openItem(BeatmapShortcut sc)
        {
            if (sc.IsFolder)
                OpenWindow(new FolderWindow(sc, this));
            else
                launchShortcut(sc);
        }

        private void launchShortcut(BeatmapShortcut sc)
        {
            if (beatmaps == null || !Guid.TryParse(sc.BeatmapId, out var id))
                return;

            var info = beatmaps.QueryBeatmap(b => b.ID == id);
            if (info != null)
                OnPlayBeatmap?.Invoke(info);
        }

        public void PlayShortcutDirect(BeatmapShortcut sc) => launchShortcut(sc);

        private void removeItem(BeatmapShortcut sc)
        {
            desktopState.Items.Remove(sc);
            save();
            rebuildIcons();
        }

        // ---- icon right-click menu (Open / Rename / Copy / Delete / Properties) -------

        private void showIconContextMenu(BeatmapShortcut sc, DesktopIcon di, Vector2 screenPos)
        {
            hideStart();
            contextMenuLayer.Clear();

            var items = new System.Collections.Generic.List<(string, Action, bool)>
            {
                (sc.IsFolder ? "Open" : "Play", () => openItem(sc), false),
                ("Rename", () => beginRename(sc, di), false),
                ("Copy", () => copyItem(sc), false),
            };

            if (!sc.IsFolder)
                items.Add(("Show ranking", () => OpenWindow(new RankingWindow(sc.BeatmapId, sc.Label)), false));

            items.Add(("Delete", () => removeItem(sc), true));
            items.Add(("Properties", () => OpenWindow(new AboutWindow()), false));

            var menu = new Win95ContextMenu(items) { Position = toIconLayerSpace(screenPos) };
            contextMenuLayer.Add(menu);
        }

        private Vector2 toIconLayerSpace(Vector2 screenPos) => contextMenuLayer.ToLocalSpace(screenPos);

        private void copyItem(BeatmapShortcut sc)
        {
            var clone = new BeatmapShortcut
            {
                Label = sc.Label + " (copy)",
                BeatmapId = sc.BeatmapId,
                IsFolder = sc.IsFolder,
                Items = new System.Collections.Generic.List<BeatmapShortcut>(sc.Items),
            };
            placeAtFreeCell(clone);
            desktopState.Items.Add(clone);
            save();
            rebuildIcons();
        }

        private void beginRename(BeatmapShortcut sc, DesktopIcon di)
        {
            contextMenuLayer.Clear();
            OpenWindow(new RenameWindow(sc.Label, newName =>
            {
                if (!string.IsNullOrWhiteSpace(newName))
                {
                    sc.Label = newName;
                    save();
                    rebuildIcons();
                }
            }));
        }

        // ---- grid placement / arranging ----------------------------------------------

        // Number of built-in program icons (kept in sync with rebuildIcons()).
        private const int program_icon_count = 14;

        // How many columns the program icons occupy, given current height.
        private int programColumns()
        {
            int perCol = Math.Max(1, (int)((DrawHeight - TASKBAR_HEIGHT - 24) / cellH));
            if (perCol <= 1 || DrawHeight <= 0)
                perCol = 8;
            return (program_icon_count + perCol - 1) / perCol;
        }

        private void placeAtFreeCell(BeatmapShortcut sc)
        {
            // Find the first free grid cell scanning top-to-bottom, then next column. User items
            // start after the columns occupied by the built-in program icons so they never overlap.
            int perCol = Math.Max(1, (int)((DrawHeight - TASKBAR_HEIGHT - 24) / cellH));
            int startCol = programColumns();

            for (int col = startCol; col < startCol + 64; col++)
            {
                for (int row = 0; row < perCol; row++)
                {
                    float x = col * cellW;
                    float y = row * cellH;
                    if (!cellOccupied(x, y))
                    {
                        sc.X = x;
                        sc.Y = y;
                        return;
                    }
                }
            }

            sc.X = startCol * cellW;
            sc.Y = 0;
        }

        private bool cellOccupied(float x, float y)
        {
            foreach (var item in desktopState.Items)
            {
                if (Math.Abs(item.X - x) < 4 && Math.Abs(item.Y - y) < 4)
                    return true;
            }
            return false;
        }

        private void snapIcon(DesktopIcon di)
        {
            // Snap to the nearest grid cell and keep on-screen.
            float maxX = Math.Max(0, DrawWidth - ModsPanel.WIDTH - cellW);
            float maxY = Math.Max(0, DrawHeight - TASKBAR_HEIGHT - cellH);

            float gx = (float)Math.Round(di.Position.X / cellW) * cellW;
            float gy = (float)Math.Round(di.Position.Y / cellH) * cellH;

            gx = Math.Clamp(gx, 0, maxX);
            gy = Math.Clamp(gy, 0, maxY);

            di.Position = new Vector2(gx, gy);
        }

        /// <summary>"Arrange Icons" — re-lay every icon into clean top-down columns.</summary>
        public void ArrangeIcons()
        {
            hideStart();
            HideContextMenu();

            int perCol = Math.Max(1, (int)((DrawHeight - TASKBAR_HEIGHT - 24) / cellH));

            // Re-pack user items into grid order, starting in the first column AFTER the built-in
            // program icons (which occupy programColumns() columns).
            int index = 0;
            int startCol = programColumns();
            foreach (var sc in desktopState.Items)
            {
                int col = startCol + index / perCol;
                int row = index % perCol;
                sc.X = col * cellW;
                sc.Y = row * cellH;
                index++;
            }

            save();
            rebuildIcons();
        }

        /// <summary>Change icon size (Small/Medium/Large) and rebuild.</summary>
        public void SetIconSize(float size)
        {
            HideContextMenu();
            desktopState.IconSize = size;
            save();
            rebuildIcons();
        }

        /// <summary>Opens a window in the workspace and registers a taskbar button for it.</summary>
        public void OpenWindow(Win95Window window)
        {
            hideStart();

            window.Position = new Vector2(40 + windows.Count * 26 % 200, 30 + windows.Count * 26 % 160);
            window.OnActivated = () => activate(window);
            window.OnClose = () => removeWindow(window);

            windows.Add(window);
            windowLayer.Add(window);

            var btn = new Win95Button { Size = new Vector2(150, TASKBAR_HEIGHT - 8) };
            btn.Action = () => activate(window);
            btn.Add(new osu.Game.Graphics.Sprites.TruncatingSpriteText
            {
                Anchor = Anchor.CentreLeft,
                Origin = Anchor.CentreLeft,
                Text = window.Name,
                Font = OsuFont.GetFont(size: 13),
                Colour = Win95.TEXT,
                Width = 132,
            });
            taskButtons[window] = btn;
            taskbarButtons.Add(btn);

            activate(window);
        }

        private void activate(Win95Window window)
        {
            foreach (var w in windows)
                w.SetActive(w == window);

            window.FadeTo(1);

            // Bring to front by re-adding last (osu! draws later children on top).
            windowLayer.Remove(window, false);
            windowLayer.Add(window);
        }

        private void removeWindow(Win95Window window)
        {
            windows.Remove(window);
            if (taskButtons.TryGetValue(window, out var btn))
            {
                taskbarButtons.Remove(btn, true);
                taskButtons.Remove(window);
            }
        }

        public void OpenBeatmapBrowser() => OpenWindow(new BeatmapBrowserWindow
        {
            OnOpenSet = OpenDifficultyPicker,
            OnPinToDesktop = AddBeatmapShortcut,
        });

        /// <summary>Open the browser so chosen difficulties are ADDED to <paramref name="folder"/>.</summary>
        public void OpenBeatmapBrowserForFolder(BeatmapShortcut folder, System.Action onChanged) => OpenWindow(new BeatmapBrowserWindow
        {
            OnOpenSet = set => OpenDifficultyPickerForFolder(set, folder, onChanged),
            OnPinToDesktop = AddBeatmapShortcut,
        });

        public void OpenDifficultyPickerForFolder(osu.Game.Beatmaps.IBeatmapSetInfo set, BeatmapShortcut folder, System.Action onChanged)
            => OpenWindow(new DifficultyPickerWindow(set, bi =>
            {
                // Add the chosen difficulty to the collection (folder) and persist.
                folder.Items.Add(new BeatmapShortcut
                {
                    Label = $"{bi.Metadata?.Title ?? "map"} [{bi.DifficultyName}]",
                    BeatmapId = bi.ID.ToString(),
                });
                save();
                onChanged?.Invoke();
            }, null));

        /// <summary>Opens the Win95 difficulty picker; double-click plays, right-click makes a shortcut.</summary>
        public void OpenDifficultyPicker(osu.Game.Beatmaps.IBeatmapSetInfo set)
            => OpenWindow(new DifficultyPickerWindow(set, bi => OnPlayBeatmap?.Invoke(bi), CreateDifficultyShortcut));

        public void OpenAbout() => OpenWindow(new AboutWindow());

        /// <summary>Opens the EMPYREAN performance benchmark.</summary>
        public void OpenBenchmark() => OpenWindow(new BenchmarkWindow());

        /// <summary>Opens the EMPYREAN performance (render scale) settings.</summary>
        public void OpenPerformance() => OpenWindow(new PerformanceWindow());

        /// <summary>Opens the FPS counter settings (size, position, visibility).</summary>
        public void OpenFpsSettings() => OpenWindow(new FpsSettingsWindow());

        /// <summary>Opens the WinAmp-style music player (drives the real MusicController).</summary>
        public void OpenWinamp() => OpenWindow(new WinampWindow());

        /// <summary>Opens the Win95 log-on / log-off dialog.</summary>
        public void OpenAccount() => OpenWindow(new AccountWindow());

        /// <summary>Opens the server switcher (connect to dev.ppy.sh or any private server).</summary>
        public void OpenServerSwitcher() => OpenWindow(new ServerSwitcherWindow());

        /// <summary>Opens the Online folder (Ranked Play / Multiplayer / Playlists / Daily Challenge).</summary>
        public void OpenOnline() => OpenWindow(new OnlineWindow(this));

        /// <summary>Opens the AOL-style chat window.</summary>
        public void OpenChat() => OpenWindow(new AolChatWindow());

        /// <summary>Opens the server global ranking (text/IRC mode).</summary>
        public void OpenServerRanking() => OpenWindow(new ServerRankingWindow());

        /// <summary>Opens the Win95-styled profile for the current local user (or any user id).</summary>
        public void OpenProfile(long? id, string name) => OpenWindow(new ProfileWindow(id, name));

        /// <summary>Opens the Win95-styled beatmap downloader (osu!direct search).</summary>
        public void OpenBeatmapDownloader() => OpenWindow(new BeatmapDownloaderWindow());

        /// <summary>Opens the AOL-styled Ranked Play queue panel (ELO/winrate + Sign On to queue).</summary>
        public void OpenRankedPlay() => OpenWindow(new RankedPlayWindow { OnQueue = () => OnOpenRankedPlay?.Invoke() });

        private ScheduledDelegate clockUpdate;

        protected override void LoadComplete()
        {
            base.LoadComplete();

            // Restore persisted desktop (icons, folders, positions, size).
            shortcutStore = new DesktopShortcutStore(storage);
            desktopState = shortcutStore.Load() ?? new DesktopState();
            rebuildIcons();
            applyWallpaper();

            updateClock();
            clockUpdate = Scheduler.AddDelayed(updateClock, 1000, true);

            // Auto-open the 2FA prompt when the API needs a verification code (otherwise the user
            // has no way to enter it on the Win95 desktop).
            if (api != null)
            {
                apiStateBindable.BindTo(api.State);
                apiStateBindable.BindValueChanged(s =>
                {
                    if (s.NewValue == osu.Game.Online.API.APIState.RequiresSecondFactorAuth && !twoFactorOpen)
                    {
                        twoFactorOpen = true;
                        OpenWindow(new TwoFactorWindow());
                    }
                    else if (s.NewValue == osu.Game.Online.API.APIState.Online)
                        twoFactorOpen = false;
                }, true);
            }
        }

        /// <summary>
        /// The teal desktop backdrop. A left-click clears any open Start/context menu; a
        /// right-click opens the Win95 desktop context menu at the cursor.
        /// </summary>
        /// <summary>
        /// A container that only receives positional input where one of its children does — empty
        /// space passes through to whatever is behind it (here, the desktop surface), so marquee
        /// selection drags on the empty desktop aren't swallowed by the full-screen icon layer.
        /// </summary>
        private partial class PassThroughContainer : Container
        {
            public override bool ReceivePositionalInputAt(Vector2 screenSpacePos)
            {
                foreach (var child in Children)
                {
                    if (child.ReceivePositionalInputAt(screenSpacePos))
                        return true;
                }

                return false;
            }
        }

        private partial class DesktopSurface : Container
        {
            private readonly Win95Desktop desktop;
            private Box selectionBox;
            private Vector2 dragStart;

            public DesktopSurface(Win95Desktop desktop)
            {
                this.desktop = desktop;
            }

            protected override bool OnClick(ClickEvent e)
            {
                desktop.hideStart();
                desktop.HideContextMenu();
                desktop.clearIconSelection();
                return true;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                if (e.Button == osuTK.Input.MouseButton.Right)
                {
                    desktop.hideStart();
                    desktop.ShowDesktopContextMenu(e.MousePosition);
                    return true;
                }

                return base.OnMouseDown(e);
            }

            // ---- rubber-band (marquee) selection -------------------------------------------------
            protected override bool OnDragStart(DragStartEvent e)
            {
                // Begin a Windows-style transparent selection rectangle from the press point.
                dragStart = e.MousePosition;
                desktop.clearIconSelection();

                if (selectionBox == null)
                {
                    Add(selectionBox = new Box
                    {
                        Colour = new Color4(49, 106, 197, 90),
                        Alpha = 0,
                    });
                }

                selectionBox.Position = dragStart;
                selectionBox.Size = Vector2.Zero;
                selectionBox.Alpha = 1;
                return true;
            }

            protected override void OnDrag(DragEvent e)
            {
                if (selectionBox == null)
                    return;

                var cur = e.MousePosition;
                var topLeft = new Vector2(Math.Min(dragStart.X, cur.X), Math.Min(dragStart.Y, cur.Y));
                var size = new Vector2(Math.Abs(cur.X - dragStart.X), Math.Abs(cur.Y - dragStart.Y));

                selectionBox.Position = topLeft;
                selectionBox.Size = size;

                // Live-select icons intersecting the rectangle (in this surface's local space).
                desktop.selectIconsInRect(ToScreenSpace(topLeft), ToScreenSpace(topLeft + size));
            }

            protected override void OnDragEnd(DragEndEvent e)
            {
                if (selectionBox != null)
                    selectionBox.Alpha = 0;
            }
        }

        private void updateClock()
        {
            if (clock != null)
                clock.Text = DateTime.Now.ToString("h:mm tt");

            if (userInfo != null)
            {
                try
                {
                    var user = api?.LocalUser?.Value;
                    string name = string.IsNullOrEmpty(user?.Username) ? "guest" : user.Username;

                    string ppRank = string.Empty;
                    var rs = rulesetBindable?.Value;
                    if (rs != null && statisticsProvider != null)
                    {
                        var stats = statisticsProvider.GetStatisticsFor(rs);
                        if (stats != null)
                        {
                            string pp = stats.PP != null ? $"{stats.PP:0}pp" : null;
                            string rank = stats.GlobalRank != null ? $"#{stats.GlobalRank:N0}" : null;
                            ppRank = string.Join(" · ", new[] { pp, rank }.Where(s => !string.IsNullOrEmpty(s)));
                        }
                    }

                    userInfo.Text = string.IsNullOrEmpty(ppRank) ? name : $"{name} · {ppRank}";
                }
                catch
                {
                    // never let the tray refresh break anything.
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            clockUpdate?.Cancel();
            base.Dispose(isDisposing);
        }
    }
}
