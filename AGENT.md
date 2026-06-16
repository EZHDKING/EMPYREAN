# AGENT.md — working on EMPYREAN as an AI agent

This file orients an AI coding agent to the EMPYREAN codebase: what it is, how it is laid
out, the hard constraints, and the conventions to follow. Read it fully before making
changes.

---

## What EMPYREAN is

EMPYREAN is a performance- and simplicity-focused fork of osu!lazer (the game) and
osu-framework (its engine), with a flat Windows 95-era UI. Motto: **"osu!lazer, but
better."** The guiding rule: a change is only worth making if it improves gameplay
performance, input latency, frametime stability, competitive reliability, or the simplicity
of the client. The UI is not the product; the gameplay is.

Full doctrine: `PROJECT.md`. Dated change log: `docs/CHANGES.md`. User-facing overview:
`README.md`.

---

## Repository layout

```
EMPYREAN/
├─ osu/                  # the game (osu!lazer fork). Assembly name: osu.Game
│  └─ osu.Game/Empyrean/ # ALL EMPYREAN-specific game code lives here
│     ├─ Desktop/        # Win95 desktop shell + windows (PerformanceWindow=EZHD Upscaler,
│     │                  #   FpsSettingsWindow, AboutWindow, BeatmapBrowserWindow, etc.)
│     ├─ UI/             # Win95 widgets (Win95Window, Win95Button, Win95CheckRow,
│     │                  #   Win95Bevel, Win95Panel/Win95 palette, Win95ContextMenu)
│     ├─ Graphics/       # EmpyreanSharpenContainer (EZHDSR), etc.
│     ├─ Overlays/       # EmpyreanControlPanel (hosts real osu! settings, 95-themed)
│     └─ Resources/      # embedded icons, wallpapers, tracks, skins (see EmpyreanAssets.cs)
├─ osu-framework/        # the engine (osu-framework fork). Publishable as EMPYREAN-FRAMEWORK
├─ osu-resources/        # fonts/samples/skins/textures/shaders. Publishable as EMPYREAN-RESOURCES
├─ docs/CHANGES.md       # dated change log — UPDATE THIS with every change
├─ PROJECT.md            # design doctrine + phase plan
├─ build_linux.sh        # Linux Release build
├─ build_windows.bat     # Windows Release build
├─ build_all.sh          # builds BOTH targets from Ubuntu 24.04 (cross-compile)
└─ run_linux.sh          # runs with Vulkan + MultiThreaded profile
```

Both `osu/osu.Game` and `osu-resources` reference `osu-framework` as **project references**
(not NuGet packages), so engine and resource changes compile into the build. All three
target **net8.0**.

---

## Hard constraints (read before editing)

1. **Build target is net8.0 everywhere.** If you add or retarget a project, it must be
   `net8.0`, or project references between them break (a netstandard project cannot
   reference the net8.0 framework project — this exact mistake has happened).

2. **The osu! analyzer treats these as ERRORS, not warnings — sweep before finishing:**
   - unused `using` directives
   - unused private members
   - setting `OsuSpriteText.Truncate`
   So: only import what you use, remove dead code, never touch the Truncate setter.

3. **Pre-existing warnings are harmless and EXPECTED — do not "fix" them blindly:** a large
   set of CS8618/CS8604/CS8600/CS0649 nullability warnings, NU1903 (MessagePack), CA1862,
   CA1845, CS8625. In particular `devBuildBanner` is intentionally a null CS0649 field (it
   suppresses the "DEVELOPER BUILD" banner). Leave these alone.

4. **Shaders live in `osu-resources/osu.Game.Resources/Shaders/` and are embedded** via
   `<EmbeddedResource Include="Shaders\**\*" />`. The framework's `ShaderManager` resolves a
   name like `"EZHDSR"` to `sh_EZHDSR.fs`. A fragment shader that replaces the default
   texture shader MUST match `sh_Texture.fs`'s structure: same includes (`sh_Utils.h`,
   `sh_Masking.h`, `sh_TextureWrapping.h`), `v_TexCoord` at **location 2**, and output via
   `getRoundedColor(...)`. The framebuffer blit path does NOT populate custom uniform blocks,
   so a post-process shader on that path must be uniform-free (derive texel size from
   `textureSize()`, bake constants).

5. **Win95 visual identity is mandatory:** flat surfaces, hard bevels, NO translucency,
   blur, parallax, gradients or drop shadows. Use the `Win95` palette and `Win95Bevel`.

6. **Never claim unverified performance numbers.** State mechanisms and caveats honestly.
   If something cannot be measured here, say so.

---

## The single most important constraint: you cannot run or compile this here

There is no GPU, no display, and the .NET SDK download is network-blocked in the agent
sandbox. **You cannot build or run the game.** Therefore:

- **Verify statically.** Use real framework APIs (grep the framework source to confirm a
  method/property exists and its signature), check brace balance on every file you touch,
  and sweep for unused usings / missing imports.
- The human builds on their own Ubuntu 24.04 machine and reports errors/screenshots. Work
  from those reports; diagnose the root cause before re-emitting code.
- **Shaders and renderer changes are the highest-risk edits** because they can only be
  validated at runtime on a GPU. A bad shader can black-screen the game with no way to tell
  which line failed. Gate risky shader features behind a toggle that is read at startup, so
  the human can disable it via config if it breaks. Do not bury unverifiable shader/renderer
  code in a large change and imply it works.

---

## Verified facts worth reusing (don't re-derive)

- **Frame-rate caps** were in `osu-framework`: `GameThread.DEFAULT_ACTIVE_HZ` (also feeds
  `maximum_sane_fps` and the ThreadRunner multithreaded rate) and
  `ThrottledFrameClock.MaximumUpdateHz`. All raised to remove the ceiling.
- **The renderer is Veldrid** (`ppy.Veldrid`), a graphics abstraction with a Vulkan backend.
  Optimizing "the Vulkan renderer" means forking Veldrid — its GPU submission is in a
  separate package; the framework only calls into it.
- **FPS gains come only from a smaller real backbuffer** (`FrameworkSetting.SizeFullscreen` /
  `WindowedSize`), which the EZHD Upscaler drives. An in-engine `BufferedContainer`
  framebuffer downscale was measured to give ZERO FPS gain and was scrapped. An in-app
  sharpen (EZHDSR) via `BufferedContainer` was measured to LOWER FPS (extra pass) and gets
  re-blurred by the monitor's hardware upscale — it is off by default.
- **GameHost IS resolvable via DI** (`Dependencies.CacheAs(this)` in GameHost). So is
  `FrameworkConfigManager`, `OsuConfigManager`, `IRenderer`, `AudioManager`. The window's
  native resolution: `host.Window.CurrentDisplayBindable.Value.Bounds` (a `Rectangle`).
- **osu! API**: `IAPIProvider` (LocalUser, State, Login/Logout/AuthenticateSecondFactor,
  Queue), `SoloScoreInfo.PP` is `double?` while `UserStatistics.PP` is `decimal?` — match
  `Math.Clamp` bound types to the value type or the wrong overload is chosen.
- **Server endpoints**: `CreateEndpoints()` in `OsuGameBase` routes known production hosts to
  `ProductionEndpointConfiguration` and `dev.ppy.sh` to `DevelopmentEndpointConfiguration`.
  Default is `dev.ppy.sh` (live-server score submission is out of scope; see README).

---

## Conventions

- **Config**: add settings to `OsuSetting` enum + a `SetDefault(...)` in
  `OsuConfigManager.InitialiseDefaults()`. EMPYREAN settings are prefixed `Empyrean...`.
  Bind with `config.BindWith(OsuSetting.X, bindable)`. `SetDefault` only applies when no
  value is already stored for that user.
- **Desktop icons**: register in `Win95Desktop` via `addProgramIcon(icon, label, slot,
  OpenX, "iconName")`, add a matching `OpenX()` method, and bump `program_icon_count`. Icons
  wrap into columns to stay on-screen; user items are placed after the program columns.
- **Windows**: subclass `Win95Window`, add content via `Add(...)`, use the `Win95` palette.
- **Formatting**: match surrounding code. Keep changes minimal and reversible. Add a brief
  comment prefixed `EMPYREAN:` explaining *why* a change helps, where it isn't obvious.
- **After any change**: update `docs/CHANGES.md` (prepend a dated section before the "##
  Build chain" marker), then re-check brace balance and imports on every touched file.

---

## Common failure modes (seen in practice)

- Editing leaves an orphaned/duplicate brace or pastes a helper method inside another method
  → always re-verify brace balance and method nesting after an edit.
- Missing `using` for: `System` (Action/Math), `osuTK` (Vector2), `osuTK.Graphics` (Color4),
  `osu.Framework.Bindables` (Bindable types), `osu.Framework.Graphics.UserInterface`
  (BasicSliderBar), `osu.Framework.Input.Events`, `System.Drawing` (Size).
- `OsuSpriteText` with `RelativeSizeAxes.X` throws unless `AllowMultiline = true`; the
  Truncate setter throws (analyzer error).
- Assuming a capability is unavailable without grepping the framework source first.
