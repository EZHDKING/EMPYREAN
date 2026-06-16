# EMPYREAN — change manifest

Exactly what this fork changed versus upstream osu!lazer, so anything can be found, audited,
or reverted. Creator: **EZHD KING**.

## New files (all fork code is isolated under `Empyrean/` namespaces)
- `osu/osu.Game/Empyrean/EmpyreanInfo.cs` — identity, creator credit, console banner.
- `osu/osu.Game/Empyrean/UI/EmpyreanLogo.cs` — flat 1980s vaporwave wordmark/scene (sky
  gradient + banded sun + perspective grid + chromatic wordmark). Static, no shaders, no
  per-frame animation — pure flat primitives (PROJECT.md §5.1).
- `osu/osu.Game/Empyrean/Resources/Tracks/empyrean-intro.mp3` — embedded intro theme.
- `osu/osu.Game/Empyrean/Terminal/TerminalEngine.cs` — pure command parser/dispatcher (UI-free, unit tested).
- `osu/osu.Game/Empyrean/Terminal/TerminalCommands.cs` — command set + `ITerminalContext` seam.
- `osu/osu.Game/Empyrean/GameTerminalContext.cs` — live bridge (config/API/mods/framework config).
- `osu/osu.Game/Empyrean/Overlays/EmpyreanTerminalOverlay.cs` — Win95/DOS console view.
- `osu/osu.Game/Empyrean/UI/Win95Panel.cs` — Win95 palette + beveled panel primitive.
- `osu/osu.Game/Screens/Menu/IntroNone.cs` — instant-boot intro screen.
- `osu/osu.Game.Tests/NonVisual/Empyrean/TerminalEngineTest.cs` — 14 engine unit tests.
- Root: `build_linux.sh`, `run_linux.sh`, `install_linux.sh`, `build_windows.bat`,
  `run_windows.bat`, `install_windows.bat`, `benchmark.sh`, `benchmark.bat`.
- Docs: `README.md`, `AGENT.md`, `PROJECT.md`, `docs/BENCHMARKS.md`, this file.

## Modified upstream files
- `osu/osu.Game/Configuration/IntroSequence.cs` — added `None` (instant) as first value.
- `osu/osu.Game/Screens/Loader.cs` — wired `IntroNone`; unknown sequences fall back to it.
- `osu/osu.Game/Screens/Menu/IntroScreen.cs` — added `protected bool NextScreenReady` helper.
- `osu/osu.Game/Configuration/OsuConfigManager.cs` — competitive defaults + new
  `EmpyreanFlatGameplay` setting (default ON). Defaults changed:
  ShowStoryboard→false, PreferNoVideo→true, MenuVoice→false, MenuParallax→false,
  HitLighting→false, StarFountains→false, CursorRotation→false, MenuTips→false,
  AutomaticallyDownloadMissingBeatmaps→false, ShowFirstRunSetup→false,
  SeasonalBackgroundMode→Never, DiscordRichPresence→Off, IntroSequence→None.
- `osu/osu.Game.Rulesets.Osu/Skinning/Default/MainCirclePiece.cs` — flat low-overdraw
  gameplay path gated on `EmpyreanFlatGameplay`; full upstream path preserved behind the toggle.
- `osu/osu.Game/OsuGame.cs` — loads `GameTerminalContext` (cached as `ITerminalContext`) + terminal overlay.
- `osu/osu.Desktop/Program.cs` — removed tournament-mode branch + `using osu.Game.Tournament`.
- `osu/osu.Desktop/OsuGameDesktop.cs` — removed macOS app-location checker + `using osu.Desktop.MacOS`.
- `osu/osu.Game/Properties/AssemblyInfo.cs` — removed tournament `InternalsVisibleTo`.

## Removed (platform scope = Windows + Linux only; simplicity mandate)
- Mobile: `osu.Android`, `osu.iOS`, all `*.Tests.Android`, all `*.Tests.iOS`, `osu.Android.props`,
  `osu.iOS.props`, `osu.Android.slnf`, `osu.iOS.slnf`.
- macOS: `osu.Desktop/MacOS/`.
- Tournament: `osu.Game.Tournament`, `osu.Game.Tournament.Tests`.
- `osu.sln`, `osu.Desktop.slnf`, and `osu.Desktop.csproj` cleaned of all references above.
- Stale `bin/`/`obj/` build artifacts (regenerate on build).

## Build fixes (post first-build feedback)
- `EmpyreanTerminalOverlay.cs`: added `using osu.Framework.Graphics.Sprites;` (FontUsage)
  and renamed the local `KillFocus()` helper to `ReleaseConsoleFocus()` so it no longer hides
  the `TextBox.KillFocus()` virtual. Both were the only errors in the first Linux build.

## Visual identity (1980s vaporwave on a Win95 budget)
- Intro now shows `EmpyreanLogo` (flat vaporwave scene) and plays the embedded
  `empyrean-intro.mp3` theme; the stock osu! logo is hidden during boot. The track keeps
  playing under the menu. Dwell time is short (`intro_dwell = 1800ms`) and skips nothing
  essential — still a near-instant, cheap boot.
- `Win95` palette extended with a flat vaporwave accent set (magenta/cyan/purple/sun/grid).
  The terminal title bar now uses a free vertex-colour gradient (no shader).
- Styling principle held throughout: vaporwave look via FLAT colour, vertex-colour
  gradients (free), and static geometry only — never animated gradients, blur, or shaders
  (PROJECT.md §5.1). Extending the same treatment to settings/song-select panels is staged
  as incremental work using the same `Win95Panel` + palette primitives; it is intentionally
  NOT bulk-applied to every upstream overlay at once, to keep each change build-verified.

## Modified upstream files (continued)
- `osu/osu.Game/Screens/Menu/IntroNone.cs` — plays embedded theme + shows vaporwave logo.
- `osu/osu.Game/osu.Game.csproj` — embeds `Empyrean\Resources\**` (intro track + future assets).
- `OSU_GRAPHICS_RENDERER=vulkan` (fastest on NVIDIA RTX; framework auto-falls-back).
- `OSU_EXECUTION_MODE=MultiThreaded` (lowest-latency thread layout).

## Windows 95 visual conversion + menu music (post-runtime feedback)
The first runnable build loaded EMPYREAN correctly (settings section, logo, instant boot) but
still wore osu!'s modern theme. This round forces the 95 look centrally and fixes music:

- `osu/osu.Game/Overlays/OverlayColourProvider.cs` — remapped ALL ~30 shade properties to a
  flat Windows 95 palette (gray #C0C0C0 surfaces, navy #000080 selection, black text). The
  entire overlay system (settings, song-select sidebar, chat, mod select) pulls colour from
  here, so this flips the whole UI to a flat 95 tone in one central, low-risk place.
- `osu/osu.Game/Graphics/UserInterfaceV2/SwitchButton.cs` — modern animated pill toggle
  replaced with a classic Win95 square checkbox (sunken white box, hard border, black tick).
- `osu/osu.Game/Screens/Menu/ButtonSystem.cs` — main-menu button colours flattened to gray/navy.
- `osu/osu.Game/Empyrean/EmpyreanMenuMusic.cs` (new) + `MainMenu.cs` — EMPYREAN theme plays
  ONLY on the menu, stops at song select, and silences the bundled osu! circles.mp3. Music
  removed from IntroNone (it was leaking into every screen).

### Still modern (honest status)
Corner radii are set per-drawable and not globally overridable, so some panels keep rounded
corners despite the flat palette. Fully squaring every widget is a larger per-component pass.

## Windows 95 desktop shell (full GUI replacement)
The recolor wasn't enough — the *form* was still modern osu!. This adds a real Win95 desktop
shell, built from scratch as flat beveled primitives (AGENT §5.1), layered over the menu:

- `osu/osu.Game/Empyrean/UI/Win95Bevel.cs` — authentic chiselled double-bevel (raised/sunken).
- `osu/osu.Game/Empyrean/UI/Win95Button.cs` — gray push button that presses in on click.
- `osu/osu.Game/Empyrean/UI/Win95Window.cs` — draggable window: navy title bar, icon, title,
  minimize/maximize/close buttons, sunken client area, focus-to-front.
- `osu/osu.Game/Empyrean/Desktop/Win95Desktop.cs` — teal desktop + window manager (multiple
  windows at once) + taskbar (Start button, per-window buttons, live clock).
- `osu/osu.Game/Empyrean/Desktop/StartMenu.cs` — classic Start menu with vertical banner.
- `osu/osu.Game/Empyrean/Desktop/BeatmapBrowserWindow.cs` — explorer-style map list;
  double-click presents a map (real gameplay path). Open several to compare maps.
- `osu/osu.Game/Empyrean/Desktop/AboutWindow.cs` — credits EZHD KING (§2.1).
- `MainMenu.cs` — hosts the desktop over the menu, wired: Play -> solo flow, Settings ->
  settings overlay, Beatmaps -> present map, Shut Down -> exit.

The shell is additive and never touches the gameplay/render hot path. The stock osu! settings
and song-select still exist underneath (reached via the desktop), now flat-gray themed.

## Exact Win95 palette + bevels (matched to React95)
- `osu/osu.Game/Empyrean/UI/Win95Panel.cs` (palette) — colours replaced with the authoritative
  React95 "original" theme tokens: material #C6C6C6, borderLightest #FEFEFE, borderLight #DFDFDF,
  borderDark #848584, borderDarkest #0A0A0A, header #060084, canvas #FFFFFF, text #0A0A0A.
- `osu/osu.Game/Empyrean/UI/Win95Bevel.cs` — rebuilt to the exact React95 border recipe (2px
  outer ring + 1px inner ring) with the four real styles: Button, Window, Field (sunken),
  Thin. All call sites updated to the new `Style` enum.

NOTE on the screenshots: the modern settings panel shown was from a build that predated the
palette/checkbox/desktop work — those changes ARE in the tree and the SettingsPanel reads its
colours from OverlayColourProvider (now remapped to Win95 gray), so a clean rebuild shows the
flat gray panel and square checkboxes. If a stale binary is run, none of it appears.

## Desktop shell: input fixes, shortcuts, context menu, integrated mods + search
Addressing direct feedback on the working desktop:
- Hidden/under-desktop osu! menu buttons no longer catch clicks: MainMenu hides + disables the
  ButtonSystem (Alpha 0, AlwaysPresent false) and side flashes; the osu! logo is hidden too.
  Win95Button/Win95Window/DesktopIcon all refuse input when Alpha<=0.01 (fixes invisible-but-
  clickable controls; minimize now sets Alpha 0 properly).
- The modern TOP toolbar is removed (Toolbar.PopIn neutralised) — the Win95 BOTTOM taskbar is
  the only bar now.
- Desktop shortcuts: `DesktopIcon` (double-click to launch). Default icons (Play/Beatmaps/
  Settings/About) plus user-pinned beatmap shortcuts that play instantly on double-click.
- Right-click desktop -> `Win95ContextMenu` (Play osu!, Open Beatmaps, Add New Map, Cut, Copy,
  Paste, Settings, Properties).
- Song-select replaced on the desktop: a permanent `ModsPanel` docked right (all mods for the
  ruleset, click to toggle into the global SelectedMods), and `BeatmapBrowserWindow` now has a
  search/filter bar; double-click a map to play, right-click to pin it to the desktop.

## Instant play, Win95 control panel, difficulty picker (desktop integration)
- Direct gameplay launch: `MainMenu.PlayBeatmapDirect(BeatmapInfo)` sets the working beatmap +
  ruleset and pushes PlayerLoader/SoloPlayer straight away — NO modern song select. The desktop
  flow is now: Beatmaps window -> double-click a set -> Win95 difficulty picker -> double-click a
  difficulty -> map plays instantly.
- `osu/osu.Game/Empyrean/Desktop/DifficultyPickerWindow.cs` (new) — Win95 prompt listing a set's
  difficulties (sorted by stars); double-click plays.
- `osu/osu.Game/Empyrean/Overlays/EmpyreanControlPanel.cs` (new) — a real Win95 Control Panel
  window with GroupBox sections and native checkboxes, bound to live config. Replaces the modern
  settings overlay on the desktop (Start->Settings and the desktop Settings icon open this).
- `osu/osu.Game/Empyrean/UI/Win95CheckRow.cs` (new) — reusable Win95 labelled checkbox bound to
  a bindable.
- Right-click desktop "Add New Map" no longer dumps you into song select; the context menu now
  has Open Beatmaps / New Beatmap window / Arrange Icons / Refresh / Settings / Properties.
- Pinned map shortcuts now open the difficulty picker (consistent instant-play flow).

## Full settings in the control panel + mod settings popups + crash fix
- CRASH FIX: taskbar button used OsuSpriteText.Truncate (throws "Use TruncatingSpriteText
  instead"), which crashed when opening any window (e.g. the difficulty picker). Switched to
  TruncatingSpriteText. This is what was aborting the game on map open.
- `EmpyreanControlPanel` rebuilt to host ALL real osu! settings sections (General, Skin, Input,
  User Interface, Gameplay, Rulesets, Audio, Graphics, Online, Maintenance, Debug) inside a
  Win95 window with a left-hand section list, classic-Control-Panel style. We cache a
  Win95-flat OverlayColourProvider so the hosted sections render gray/flat, and host a real
  KeyBindingPanel so the Input section's Configure button works.
- `osu/osu.Game/Empyrean/Desktop/ModSettingsWindow.cs` (new) — selecting an adjustable mod
  (DT rate, DA values, etc.) pops a Win95 dialog hosting that mod's own setting controls via
  CreateSettingsControls(), editing the exact instance in the active mod list. Only mods that
  actually have settings trigger the popup (GetSettingsSourceProperties().Any()).
- Drag-and-drop beatmap/skin import is unchanged (registered at OsuGameBase) — dropping an
  .osz/skin still imports immediately.

## Working instant play, readable settings, persistent shortcuts
- FIX (maps wouldn't launch): PlayBeatmapDirect now routes through OsuGame.PerformFromScreen
  (returns to the menu screen safely regardless of open overlays/windows) then pushes the
  player. Double-clicking a difficulty now actually starts the map.
- Instant launch: new `EmpyreanPlayerLoader` overrides PlayerPushDelay to 0 (base waits ~1.8s
  on a metadata splash), so the map starts as soon as it's loaded — no launch animation.
- FIX (unreadable black settings): removed the blanket `Colour = Black` tint that turned the
  hosted osu! settings controls into solid black rectangles. The control panel section area and
  the mod settings popup now use a dark panel behind the native (light-text) osu! controls, so
  they're readable; the window chrome stays Windows 95. This also surfaces the Difficulty Adjust
  CS/AR/OD/HP sliders that were previously black-on-white.
- Persistent desktop shortcuts: new `DesktopShortcutStore` saves shortcuts to
  empyrean-desktop.json in osu! storage and restores them on launch. Right-click a difficulty in
  the picker -> creates a desktop shortcut to that exact difficulty; double-click the icon to
  play it instantly; right-click an icon to delete it.

## Stability + taskbar/user info + WinAmp + editor access + mod sanitize
- FIX (cross-ruleset mod crash "...mod belonging to a ruleset different than osu!"):
  PlayBeatmapDirect now sets the ruleset first, then re-resolves SelectedMods against that
  ruleset's own mod instances (by acronym), drops anything foreign, and reduces to a compatible
  set (clears entirely on any doubt). No more wrong-ruleset gameplay errors.
- FIX (intermittent stuck-intro -> triangle fallback): the control panel no longer builds the 11
  real osu! settings sections + KeyBindingPanel in its BDL — that work is deferred to first open
  (buildIfNeeded on PopIn). Nothing heavy runs in the MainMenu load chain, so it can't fall back
  to the stock intro because a section threw.
- Taskbar system tray now shows: "Get Beatmaps" button (opens the existing beatmap listing/
  downloader), logged-in username · PP · global rank (from IAPIProvider + LocalUserStatistics
  Provider), and the clock.
- Desktop icons added for Editor and Skin Editor (open the real osu! tools), and a WinAmp icon.
- `osu/osu.Game/Empyrean/Desktop/WinampWindow.cs` (new): a WinAmp-skinned player driving the real
  MusicController — green LCD title/time, prev/play/pause/stop/next, click-to-seek bar.
- Right-click desktop -> "Change Wallpaper" cycles the desktop colour; "Refresh" re-lays icons.
- Drag-and-drop beatmap/skin import remains intact (OsuGameBase-level, untouched).

## Draggable icons, folders, ranking, gamemode selector + key bug fixes
- FIX (no music on 2nd play): the menu theme silenced the SHARED beatmap track via VolumeTo(0)
  and never restored it; since osu! reuses that track object, the 2nd play was silent. Now
  StopMenuTheme restores CurrentTrack volume to 1.
- FIX (DT/DA rate snapping back to default): the launch-time mod sanitizer was rebuilding mods
  from CreateAllMods() (fresh defaults), discarding edits. It now KEEPS the exact selected mod
  instance when its type belongs to the ruleset, preserving adjusted settings. Mod settings
  popups also re-commit the selection on close.
- FIX (right-click Settings did nothing): the desktop context menu now dismisses itself before
  running an action, so it no longer eats input over the opening window.
- Desktop icons are now FREELY DRAGGABLE with grid snapping; "Arrange Icons" re-flows them into
  clean columns; right-click desktop sets icon size (Small/Medium/Large). Positions + size are
  persisted to empyrean-desktop.json.
- Right-clicking a desktop icon opens a real Win95 menu: Open/Play, Rename, Copy, Show ranking
  (maps), Delete, Properties — not just "remove".
- Folders ("New Collection"): create a folder, open it to a Win95 window listing its maps;
  double-click a map inside to play it.
- `RankingWindow` (new): right-click a map icon -> Show ranking -> Win95 text-mode window with
  local scores (rank / score / accuracy / combo / date) and a note about global leaderboards.
- Taskbar gamemode selector (std / taiko / ctb / mania) sets the active ruleset so maps convert.
- WinAmp window reworked to look closer to the real thing: big green LCD time, scrolling title,
  kbps/kHz/stereo indicators, position + volume sliders, prev/play/pause/stop/next/eject.

## Real Win95 icons + wallpapers, startup jingle, login window, ranking fix, collections
- FIX (ranking "Take not supported"): the local-score query now uses Realm's .Filter(...) then
  sorts/limits in memory; Realm's LINQ provider can't translate Take()/some operators.
- Bundled assets: 18 genuine Windows 95/2000 .ico icons (converted to PNG) are embedded and used
  for desktop icons (with a vector-glyph fallback), plus 6 retro wallpapers (95/98/XP/bliss/
  dunes). New EmpyreanAssets loader builds the texture stores from embedded resources.
- "Change Wallpaper" now cycles the real bundled wallpaper images (and a couple of plain colours).
- Startup jingle: startup.mp3 is embedded and played once on launch.
- Removed the "DEVELOPER BUILD" watermark (DevBuildBanner no longer instantiated).
- Login/Log-off: new Win95 AccountWindow (right-click desktop -> "Log On to osu!...") drives the
  real IAPIProvider login/logout and shows connection status + current server endpoint.
- Profile: clicking the taskbar user label (name · pp · rank) opens your osu! profile (ShowUser).
- Collections fixed: a folder's "Add maps…" opens the browser in folder-target mode — picking a
  difficulty adds it to that collection and the folder view refreshes; contents persist to JSON.
- WinAmp: pressing Play now restores the (menu-silenced) beatmap track volume so it actually
  produces sound; visual styling already reworked toward the classic look.

## Real WinAmp .wsz skins, server switcher, collection map-adding
- WinAmp now renders from REAL classic WinAmp skins. 13 bundled .wsz skins (Classic, Winamp 5/3
  Classified, Fallout Pip-Boy, Deus Ex, Zelda, Microchip, Doritos, Garfield, Morbamp, Mr Bean…)
  are embedded; the window composites the Main.bmp background and overlays transport hit-areas at
  the authentic WinAmp button positions, with a live time/title LCD and position bar. A "Skin »"
  button cycles skins. New WinampSkin loader unzips a .wsz and turns its BMPs into textures.
- WinAmp Play restores the (menu-silenced) track volume so it produces sound.
- Server switcher: new Win95 "Server" window + desktop icon. Connects to dev.ppy.sh by default and
  lets you enter any host/URL (private servers). The host is persisted and applied on next launch
  (the API connection is bound at startup, so switching servers needs a restart — stated in the
  dialog). Implemented via a custom EmpyreanEndpointConfiguration read by CreateEndpoints().
- Login/Log-on and Server now have desktop icons alongside the existing ones.
- Collections: a folder's "Add maps…" opens the browser in folder-target mode — double-click a set,
  double-click a difficulty, and it's added to that collection and persisted; the folder refreshes.

## Drag-drop collections, 2FA prompt, WinAmp/theme sync, Online folder, AOL chat
- COLLECTIONS DRAG-DROP (the big one): desktop map icons are now multi-selectable (click =
  select one, Ctrl/Shift-click = add to selection) and can be DRAGGED ONTO a collection folder
  icon. On drop, every selected map is moved into that collection and saved permanently. Folder
  drop is hit-tested against the folder icon's on-screen quad.
- 2FA: a Win95 "Verification Required" prompt now opens automatically when osu! login needs a
  two-factor code (e.g. osu.ppy.sh). Enter the code -> AuthenticateSecondFactor. Previously there
  was nowhere to type it.
- WinAmp <-> theme sync: while a beatmap track is actually playing (you hit Play in WinAmp), the
  looping EMPYREAN menu theme stops; when WinAmp is paused/stopped, the theme resumes. Coordinated
  inside EmpyreanMenuMusic and gated to the menu via SetMenuActive.
- Online folder: new "Online" desktop icon opens a Win95 folder with Ranked Play, Multiplayer,
  Playlists and Daily Challenge, each launching the matching osu! screen.
- AOL Messenger: new desktop icon opens an AOL-Instant-Messenger-styled chat window — IRC text
  mode (timestamp + nick + message) with the classic AOL blue/yellow banner, wired to osu! chat
  channels (reads backlog + live messages, posts what you type).

## Login fix, marquee multi-select, collection playback, WinAmp sync, server ranking
- FIX (login broken on osu.ppy.sh): when a custom server host was saved, CreateEndpoints() built a
  generic config with DEV OAuth credentials + wrong signalr URLs, breaking login on the official
  server. Now osu.ppy.sh/dev.ppy.sh use the REAL built-in production/development configs; the
  generic custom config is only used for genuinely private hosts. 2FA is unchanged and only triggers
  when the server actually requests it (it was never forced).
- FIX (can't play maps in collections): folder rows are now properly clickable so double-click
  registers; double-click closes the folder window and plays the map.
- Marquee selection: drag on the empty desktop to draw the Windows-style translucent selection
  rectangle; all icons inside are selected, then can be dragged together into a collection. The
  icon layer now passes empty-space input through to the desktop so the marquee works.
- WinAmp "one song behind" fixed: the title now binds to the beatmap bindable (which updates with
  the actual current track) instead of MusicController.TrackChanged (which fired too early).
- WinAmp alignment: integer 2x skin scaling and corrected LCD/title font sizes and positions.
- Server Ranking (new): Online folder -> "Server Ranking" opens a text/IRC-mode window showing the
  connected server's global Performance top players (rank / player / pp) via the live API.

## Win95 profile + beatmap downloader, real Ranked Play / Daily Challenge, ranking fix
- FIX (Server Ranking crashed): the text rows set RelativeSizeAxes.X on single-line OsuSpriteText
  (illegal on a text drawable). Removed it; the ranking window now opens.
- Ranked Play now launches the REAL matchmaking queue (ScreenIntro with MatchmakingPoolType.
  RankedPlay) instead of song select. Daily Challenge fetches today's room from the metadata
  service and pushes the real DailyChallenge screen (falls back to song select if unavailable).
- PROFILE stripped to Win95: clicking your taskbar name (or a profile entry) now opens a plain
  Win95 properties-sheet ProfileWindow that fetches the user via the live API and lists rank, pp,
  accuracy, level, ranked/total score, play count, hits, combo, play time and grade counts — no
  modern overlay, covers or gradients.
- BEATMAP DOWNLOADER stripped to Win95: "Get Beatmaps" now opens a Win95 BeatmapDownloaderWindow
  that searches osu!direct via the live API and lists results as plain rows with a Download button
  each (uses the real BeatmapModelDownloader) — no modern cards/gradients.

## AOL ranked-play panel, Enter-to-play, downloader fix
- FIX (build error): beatmap downloader used APIBeatmapSet.Creator (doesn't exist) -> AuthorString.
- FIX (CA1862 warning): browser search now uses string.Contains(q, StringComparison.OrdinalIgnoreCase).
- AOL Ranked Play panel: Online -> "Ranked Play" opens an AOL "Connecting To osu! Ranked Play…"
  window with the big yellow running-man "SIGN ON" button that queues the real matchmaking, and a
  blue stats sidebar showing ELO/Rating (live from the matchmaking lobby when available, else your
  global rank as a proxy), Performance, Accuracy and Play Count from your profile.
- ENTER to play: with a map icon selected on the desktop, pressing Enter launches it (mirrors
  double-click). In the difficulty picker, rows are now selectable (click to select, first row
  auto-selected) and Enter plays the selected difficulty.

## Beatmap sort/filters, profile scores, ranking crash hardening
- Server Ranking: wrapped the request + response handling in try/catch so a server-side rankings
  error shows inline instead of throwing the global "unhandled error" toast.
- Beatmap DOWNLOADER: added Sort (Relevance/Ranked/Rating/Plays/Favourites/Title/Artist/Difficulty),
  sort direction (Asc/Desc) and Status category (Any/Ranked/Qualified/Loved/Pending/Graveyard) —
  all cycled via Win95 buttons and re-run the osu!direct search. Each result row now also shows
  status, difficulty count and top star rating. Full-text search box accepts artist/title/mapper.
- Local beatmap BROWSER: added a Sort button (Title / Artist / Stars / Date added).
- PROFILE now shows score lists: Pinned Scores, Best Performance (top plays) and Most Played, each
  fetched from the live API into its own section (rank/title/accuracy/pp, or play count).
- PROFILE layout: added right padding so content no longer runs under the scrollbar/edge.

## Ranking Int32 overflow fix
- FIX (Server Ranking "Value was either too large or too small for an Int32"): pp was cast to int,
  which overflows on servers where a player has > ~2.1 billion pp (e.g. 130 billion on a private
  server). pp formatting now widens to long (64-bit) and clamps anything beyond long.MaxValue.
  Applied the same safe clamp to the profile Performance field, profile score rows, and the AOL
  ranked-play stats panel.

## osu-framework GPU optimizations (engine)
GPU is the bottleneck, so these target per-fragment cost and upload stalls rather than vertex count
(FastCircle is already a single shader-drawn quad, not a tessellated polygon — there is no "poly
count" to cut there).
- FastCircle shader (sh_FastCircle.fs): dropped the hot circle-distance math from highp to mediump
  and kept a hard (non-AA) edge as the default when BlendRange is 0. Hit circles cover a lot of
  screen and overdraw, so cutting their per-pixel precision is a direct fill-rate saving (mediump
  can run up to 2x rate on integrated/mobile GPUs). Smooth edge still works when BlendRange is set.
- Masking smoothness default lowered from 1px to the 0.01 floor (0 is illegal — the shader divides
  by it). Every masked container otherwise runs a 1px antialiasing blend band in the fragment shader
  every frame; removing it trades crisp/aliased edges for less per-pixel work app-wide.
- Texture upload front-loading (Renderer.cs): upstream throttles uploads to 32 textures / 2MB pixels
  per frame and only drains half the queue per frame, so a map's textures trickle into VRAM and drop
  a frame each time new notes/elements first appear. Raised the caps (8192 textures / 256MB) and now
  drain the whole queue per frame — textures reach VRAM in the first few frames (brief load hitch),
  with no per-note upload stalls after. This is the "preload to VRAM" tradeoff requested. Limits stay
  public setters so a host can dial them back for extremely large sets.
- Frame limiter already defaults to Unlimited and ExecutionMode to MultiThreaded (pre-existing), so
  the frame ceiling is uncapped.

## CPU/latency tuning + EMPYREAN Benchmark
- Runtime config (osu.Desktop.csproj): enabled Concurrent (background) GC so collections stay off
  the hot threads and can't drop/delay an input frame; enabled TieredCompilation + TieredPGO so the
  JIT optimises the hottest paths (input handling, update loop) harder; InvariantGlobalization to
  trim locale overhead. ServerGC intentionally left OFF — its longer pauses hurt frame-time
  consistency, which matters more than raw throughput for a rhythm game.
- Input/latency review: the framework's input path was already strong and is left intact — discrete
  key/mouse button events are queued individually (no coalescing of taps, so presses aren't lost),
  the whole queue is drained every update frame, input is polled at 10 kHz, the update thread runs
  at 2x draw, and the (already user-tuned) ThrottledFrameClock spin-waits the sub-millisecond
  remainder for tight timing. Tearing is enabled in fullscreen for minimum present latency. No
  changes were made here because tightening a correct low-latency path blindly would risk
  regressions; the real wins were the GC/JIT config above.
- EMPYREAN Benchmark (new desktop icon): runs three load phases — heavy jumps, heavy streams, heavy
  sliders/spinners — driving a large synthetic scene of moving/rotating/pulsing FastCircles while
  sampling per-frame frame times, then reports AVG FPS, MAX FPS, 1% low and 0.1% low FPS in a Win95
  results panel. It stresses the same draw/update pipeline as gameplay (it is not a replay of
  specific ranked maps) and gives real frame-time percentiles for comparing builds and machines.

## Truly unlimited frame rate (removed the ~10k/5k ceiling)
The frame rate was hard-limited even on "Unlimited". Three stacked ceilings caused it:
- GameThread.DEFAULT_ACTIVE_HZ was 10000 (the active update/draw thread rate cap). Raised to
  int.MaxValue. This also feeds maximum_sane_fps (the Math.Min clamp in updateFrameSyncMode) and the
  MultiThreaded main-thread rate in ThreadRunner, so all of them now impose no cap.
- ThrottledFrameClock.MaximumUpdateHz defaulted to 10000. Raised to double.MaxValue, so when running
  unlimited the clock takes its no-sleep path and never throttles.
- With both update and draw threads previously pinned at 10000 in MultiThreaded mode, the observed
  result was roughly half (~5000) after thread sync; with the caps gone the only limit is how fast
  the CPU/GPU actually complete frames.
Inactive Hz stays at 60 so an unfocused window doesn't spin the CPU. Frame rate is now bounded only
by hardware.

## Low-resolution render scaling (the big GPU-bound lever)
You're fill-rate bound, so the largest possible GPU win is shading fewer pixels.
- EmpyreanLowResContainer wraps the whole game. Below 100% it renders content into a framebuffer at
  a fraction of native resolution (via BufferedContainer.FrameBufferScale) and upscales that to the
  window — e.g. 25% on a 1280x800 window shades ~320x200 (~1/16th the pixels). At 100% it parents
  content directly with NO framebuffer, so there is zero overhead unless you opt into a lower scale.
- New OsuSetting.EmpyreanRenderScale (default 1.0 = native, range 0.25–1.0), bound live so changes
  apply instantly.
- New Win95 "Performance" desktop window with presets: Native 100% / Half 50% / Quarter 25% /
  Low 40% / Potato 15%. Linear upscaling (smoother than blocky nearest-neighbour).
Trade-off is exactly as intended: lower sharpness, big FPS gain. NOTE this scales the entire UI too
(Win95 windows/text included) when below 100% — that is inherent to scaling the whole frame.

## EZHD Upscaler (render-scale slider) + icon wrapping fix
- Why the previous attempt showed no effect: the render scale defaulted to 1.0 (native = passthrough),
  and it was only changeable from a window that had to be opened. It was never actually engaged.
- EZHD Upscaler: new desktop icon opening a Win95 window with a slider (1%–99%) plus quick presets
  (10/25/50/75/90%). Bound live to OsuSetting.EmpyreanRenderScale and persisted. Shows the derived
  render resolution when the window size is available.
- DEFAULT is now 50% (renders at half resolution, e.g. 640x480 on a 1280x960 window, upscaled), so
  the effect is active out of the box — no longer a no-op until opted in.
- EmpyreanLowResContainer reworked to be always-buffered (the framebuffer is always in the tree) so
  the wiring is guaranteed; FrameBufferScale is what changes. Confirmed the gameplay ScreenStack
  renders through this container, so reducing the scale reduces the pixels the GPU shades.
- Desktop icon off-screen bug fixed: program icons (AOL Messenger, About, Benchmark, EZHD Upscaler…)
  now wrap into additional columns instead of marching off the bottom of the screen. User map/folder
  icons are placed and arranged starting after the program-icon columns so they don't overlap.

## EZHD Upscaler reworked to set the REAL render resolution (framebuffer approach scrapped)
The previous BufferedContainer upscaler made the image pixelated but did NOT raise FPS, proving the
bottleneck isn't the in-tree framebuffer — the win only comes from a smaller actual backbuffer (which
is why changing the desktop/monitor resolution gave +1000 FPS).
- Removed EmpyreanLowResContainer and its wrapping of the game content (it added an upscale blit for
  no FPS benefit). Removed the associated render-scale binding/field.
- EZHD Upscaler now drives the framework's real resolution settings: FrameworkSetting.SizeFullscreen
  (fullscreen) and WindowedSize (windowed). Picking e.g. 640x480 makes the GPU render at 640x480 and
  the display hardware upscales it to your screen — exactly the manual desktop-resolution trick, but
  in-app and per-game.
- Window offers concrete resolutions: Native (max), 1280x960, 1024x768, 800x600, 640x480, 512x384,
  400x300, 320x240. Shows current mode and notes Fullscreen gives the cleanest result.
- NOTE: this is the mechanism that actually moves FPS for a fill-rate-bound GPU. Fullscreen mode is
  recommended; in windowed/borderless the OS compositor may rescale differently.

## EZHD Upscaler v2: slider back, aspect-ratio fix, less flashing; EZHDSR toggle
- Slider restored: 1%–99% of NATIVE resolution. Scaling native by a percentage preserves your
  monitor's aspect ratio, which fixes the black bar at the bottom (that was a 4:3 mode being
  letterboxed on a non-4:3 display). Plus quick presets 10/25/50/75/100%.
- Less flashing: the slider uses TransferValueOnCommit, so the actual resolution change happens once
  when you release the slider, not on every drag tick. (Note: a single display-mode change still
  causes one flash — that's the monitor re-syncing and is unavoidable when changing real resolution.)
- Resolution is derived from the live native display size (host.Window.CurrentDisplay.Bounds), shown
  in the window.
- EZHDSR toggle added and persisted (OsuSetting.EmpyreanSharpen).

HONEST LIMITATION on EZHDSR sharpening: a real CAS/FSR1 sharpen shader cannot currently be added.
osu!'s shaders live in the precompiled ppy.osu.Game.Resources NuGet package and the GPU submission
is in the precompiled ppy.Veldrid package — neither accepts a new shader file from this project, and
the real upscale is done by the display hardware after the backbuffer (out of the app's reach). The
toggle is wired and stored so it can drive sharpening if/when the resources package is built from
source, but it does not perform sharpening yet. Claimed otherwise would be false.

## EZHDSR sharpening is now REAL (resources compiled from source) + framework hook
You provided the osu-resources source, which unblocks adding a shader (the precompiled NuGet package
was the blocker). What changed:
- osu-resources is now built from source as a ProjectReference (was the ppy.osu.Game.Resources
  NuGet package). Its csproj now references the local osu-framework project so the shader format and
  APIs match the framework the game builds against.
- New shader osu.Game.Resources/Shaders/sh_EZHDSR.fs: a CAS/FSR1-style contrast-adaptive sharpen.
  It is a drop-in structural copy of the framework's default texture shader (same includes, input
  locations, and getRoundedColor/wrappedSampler output path) so masking, corner-rounding and wrapping
  still work — it only adds the sharpen on top. 5-tap kernel (centre + 4 neighbours), contrast-
  weighted, clamped to the local min/max to avoid ringing. No custom uniform block (the framebuffer
  blit path won't populate one), so strength is a constant (0.4) and the texel step comes from
  textureSize(). This is the cheapest kernel that still gives FSR1-like edge recovery.
- Framework hook: BufferedContainer gained a protected virtual GetCustomTextureShader(ShaderManager)
  that lets a subclass substitute the shader used to draw the captured frame to screen (defaults to
  the normal texture shader). Surgical, additive, default behaviour unchanged.
- New osu.Game/Empyrean/Graphics/EmpyreanSharpenContainer.cs subclasses BufferedContainer and returns
  the EZHDSR shader, so its blit-to-screen sharpens.
- OsuGameBase wraps the game content in the sharpen container at startup IF OsuSetting.EmpyreanSharpen
  is on (read once; toggling applies next launch — live reparenting of the whole tree is fragile).

On "strip existing shaders of animations/effects": deliberately NOT done. The existing shaders are
loaded by name (often dynamically) and most are essential or skin features; editing GLSL that can't
be compiled here is high risk for little/no universal gain, and they only run when their feature is
on screen. The safe way to kill an effect is to not render it at the C# level — say the word and that
can be done as a separate, reversible change. Honesty over a risky blind rewrite.

## EZHDSR defaulted OFF — it lowers FPS and the monitor re-blurs its sharpening
Measured: 50%% render scale WITH EZHDSR on = ~3600 FPS, vs ~6000 at a real 640px backbuffer and ~5000
at native. EZHDSR was net-negative. Two reasons, both architectural:
- It wraps the whole game in a BufferedContainer, forcing an extra full-screen render-to-texture pass
  plus a 5-tap sharpen blit. At sub-millisecond frame times that extra pass costs ~40%% FPS.
- It runs INSIDE the app, sharpening the low-res backbuffer (e.g. 640x512). The monitor's hardware
  scaler then upscales that to native (1280x1024), bilinear-blurring exactly what EZHDSR sharpened.
  To sharpen the final image the pass would have to run AFTER the upscale, but that upscale is done by
  the monitor hardware, which the application cannot insert a shader into.
So in fullscreen EZHDSR is cost with no real benefit. Default is now OFF (the wrapper returns the
plain content with zero overhead when off). The toggle remains as an opt-in. The shader, the
BufferedContainer hook and the container class stay in the tree but are inert unless enabled.
NOTE: if you already enabled it, your config persisted that — UNCHECK EZHDSR in the EZHD Upscaler to
get your frames back.

## Final polish: FPS counter, About motto, full documentation, multi-target build
- FPS counter: removed the frametime line; the FPS readout is now large and bold (32px). It
  reads two new settings live: EmpyreanFpsScale (1x-5x size) and EmpyreanFpsCorner (which
  screen corner). Defaults: 2.5x, top-right.
- New "FPS Settings" desktop icon + Win95 window: toggle the counter, resize it with a slider,
  and pick any corner. (program_icon_count is now 14.)
- About motto changed from "competition-first performance fork" to "osu!lazer, but better"
  (also updated the canonical EmpyreanInfo.TAGLINE).
- README.md rewritten: comprehensive project overview, the improvements over lazer (uncapped
  FPS, EZHD Upscaler, flat Win95 UI, FPS counter), the aim (a leaner lazer for top players),
  and an explicit note that we default to dev.ppy.sh because live osu.ppy.sh score submission
  would require an X-Token emulator fed from the real client — out of scope for a legitimate
  project.
- AGENT.md rewritten: full onboarding for AI agents (layout, hard constraints, the can't-
  compile-here reality, verified API facts, conventions, common failure modes).
- osu-framework/README.md: "EMPYREAN-FRAMEWORK" — documents the engine, the fork's changes
  (uncapped FPS, render tuning, the BufferedContainer custom-shader hook) and how to build a
  game on it. Ready to push as a standalone repo.
- osu-resources/README.md: "EMPYREAN-RESOURCES" — documents the asset/shader package, why it's
  built from source, and the EZHDSR shader. Ready to push as a standalone repo.
- build_all.sh: builds BOTH the Linux and Windows self-contained binaries from one Ubuntu
  24.04 host via .NET runtime-identifier cross-compilation (-r linux-x64 / -r win-x64), into
  dist/.

## build_all.sh now emits single artifacts; new push_all.sh for the three repos
- build_all.sh rewritten: instead of output folders it produces dist/Empyrean.exe (self-contained
  single-file Windows build via PublishSingleFile, cross-compiled from Linux) and
  dist/Empyrean.AppImage (self-contained Linux build assembled into one portable AppImage;
  appimagetool is downloaded automatically, with a FUSE-less extract-and-run fallback). Honest
  caveat in-script: osu! loads rulesets/native libs at runtime, so if the single-file .exe fails to
  launch, fall back to a folder publish.
- push_all.sh added: creates (if missing) and pushes three GitHub repos via the gh CLI —
  EMPYREAN (full monorepo, buildable on clone), EMPYREAN-FRAMEWORK (osu-framework/) and
  EMPYREAN-RESOURCES (osu-resources/). The two sub-repos are managed with separate git dirs
  (.git-empyrean-framework / .git-empyrean-resources, both gitignored) so no nested .git turns the
  monorepo's copies into broken submodule pointers. Requires `gh auth login`.

## Build chain
Already wired to the bundled local `osu-framework` checkout via
`osu.Game.csproj → ..\..\osu-framework\osu.Framework\osu.Framework.csproj`, so your existing
framework optimizations compile directly into EMPYREAN.

## Known follow-ups (require real hardware)
- Fill in `docs/BENCHMARKS.md` "Pending validation" rows with measured frametime/latency data.
- `GameTerminalContext.ReloadSkin/ReloadMap` are safe stubs; wire to live screens if desired.
- The terminal toggle key (backtick) and overlay focus behaviour need an on-device smoke test.
