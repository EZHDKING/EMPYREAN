# EMPYREAN

**osu!lazer, but better.**

[![SHOWCASE](https://img.youtube.com/vi/nD2LMgIkZzM/hqdefault.jpg)](https://www.youtube.com/watch?v=nD2LMgIkZzM)

> Creator: **EZHD KING**
> DEDICATED TO TERRY A. DAVIS

# “An idiot admires complexity, a genius admires simplicity, a physicist tries to make it simple, for an idiot anything the more complicated it is the more he will admire it, if you make something so clusterfucked he can't understand it he's gonna think you're a god cause you made it so complicated nobody can understand it. That's how they write journals in Academics, they try to make it so complicated people think you're a genius”

- Terry A. Davis

EMPYREAN is a fork of [osu!lazer](https://github.com/ppy/osu) rebuilt for top players. It
exists to do three things the upstream client does not do well enough for high-level play:

1. **Remove the frame-rate ceiling.** Stock lazer hard-limits the frame rate; EMPYREAN
   removes those caps so the game runs as fast as the hardware allows.
2. **Strip the bloated UI.** The modern lazer interface spends a large fraction of the
   frame budget on animation, blur, parallax and overlays. EMPYREAN replaces it with a
   flat, static Windows 95-style desktop that spends almost the entire budget on gameplay.
3. **Keep everything simple.** Fewer moving parts, fewer surprises, less to go wrong in a
   competitive session. *An idiot admires complexity; a genius admires simplicity.*

If a change does not improve gameplay performance, input latency, frametime stability or
competitive reliability, it does not belong here. The UI is not the product — the gameplay
is the product.

---

## What we aimed for

A leaner, faster lazer for players who care about frames and latency above visuals. The
target user already knows osu!; they do not need tutorials, animated menus, or a social
feed. They need the map to load, the cursor to track perfectly, and the frame rate to be
high and stable. EMPYREAN is built around that user and nothing else.

---

## Why EMPYREAN over other practice clients (e.g. McOsu)

McOsu is a well-known osu! practice client, but it is a separate reimplementation built on
its own engine and primarily distributed through Steam. EMPYREAN takes a different approach
that matters for serious practice:

- **1:1 with lazer gameplay.** EMPYREAN is a *direct fork of osu!lazer running on the native
  osu!framework engine*. The hit detection, timing, judgement, mod behaviour and feel are
  literally lazer's — not an approximation. If you compete on lazer, you are practising on
  the exact engine you play on, with nothing to re-learn.
- **No Steam required.** EMPYREAN is a standalone client. There is no storefront, launcher
  or platform dependency between you and the game — just the binary.
- **Performance- and simplicity-first.** The whole client is stripped to gameplay, with the
  frame-rate caps removed and a render-scale lever for fill-rate-bound machines.

This is not a knock on McOsu, which is excellent at what it does. EMPYREAN simply targets a
specific niche: a top player who wants lazer-identical practice, maximum frames, and zero
bloat, without a platform in the way.

---

## What we improved over osu!lazer

This is a summary. See `docs/CHANGES.md` for the full, dated change log and `PROJECT.md`
for the design doctrine.

### Truly uncapped frame rate
Stock lazer pins the frame rate well below what the hardware can do (a ~10,000 Hz internal
ceiling, and in practice much lower in multithreaded mode). EMPYREAN removes the stacked
caps in the engine:
- `GameThread.DEFAULT_ACTIVE_HZ` raised to effectively unlimited (this also feeds the
  internal "sane FPS" clamp and the multithreaded main-thread rate).
- `ThrottledFrameClock.MaximumUpdateHz` raised so the clock never throttles when running
  unlimited.
- The frame rate is now bounded only by how fast the CPU/GPU actually complete frames.

> Honest note: past your monitor's refresh rate and the input poll rate, extra frames buy
> very little and mostly generate heat. Uncapping removes an artificial floor on what you
> can achieve; it is not about chasing meaningless numbers.

### EZHD Upscaler — real render-resolution scaling
Players who are GPU/fill-rate bound gain the most from rendering fewer pixels. EMPYREAN's
**EZHD Upscaler** sets the *real* render resolution as a percentage of your native display
resolution (1%–99%, default 50%) and lets the display hardware upscale it. Because it
scales a percentage of native, the aspect ratio is preserved (no letterbox black bars), and
changes apply when you release the slider so the display only re-syncs once.

> Honest note: this is the mechanism that actually moves FPS, and it works best in
> **Fullscreen**. Lower percentages look softer — there is no free lunch. An in-engine
> sharpening pass (EZHDSR) exists as an opt-in but is **off by default**, because in
> fullscreen the monitor's hardware upscale happens *after* the app and re-blurs anything
> the app sharpened, while the extra render pass costs frames. Use 70–80% scale for a good
> sharpness/FPS balance.

### Flat, static Windows 95 UI
The entire modern shell is replaced with a Windows 95-era desktop: flat surfaces, hard
bevels, no translucency, blur, parallax, gradients or drop shadows. Instant boot (no intro
animation), flat gameplay pieces, a flat colour palette, and a desktop of program icons
(Play, Beatmaps, Settings, Editor, Skin Editor, WinAmp, Server, Log On, Online, AOL
Messenger, About, Benchmark, EZHD Upscaler, FPS Settings). All real osu! features
(login/2FA, profiles, rankings, beatmap download, chat, matchmaking) are reachable, just
restyled to the 95/AOL aesthetic.

### Big, customizable FPS counter
The on-screen counter shows a large, bold FPS readout (the frametime line is removed). A
**FPS Settings** window lets you toggle it, resize it (1x–5x) and place it in any screen
corner, all live.

### Engine and runtime tuning
- Render pipeline: anti-aliasing blend bands removed app-wide for hard edges (matches the
  flat aesthetic and saves fill cost), VRAM uploads front-loaded.
- GC tuned for latency, not throughput: concurrent (background) GC on, server GC off,
  Tiered PGO on, invariant globalisation on.
- Launches with the Vulkan renderer and the multithreaded execution mode for the lowest
  CPU submission cost and input latency on modern hardware.

### Improved Access to Beatmaps
- You can create collections / delete them / sort them / rename them / group them extremely easy with the simplistic Windows 95/98/XP GUI Everyone is familiar with. Everything is crystal clear and simplistic, nothing is complicated.

---

## Server connectivity (important)

**By default EMPYREAN connects to `dev.ppy.sh`** (the osu! development/test server), not the
live `osu.ppy.sh`. You can switch servers in the Server window.

This is a deliberate scope decision. Submitting scores on the **live** `osu.ppy.sh` requires
a valid request-signing token ("X-Token") that the official client generates. Producing
those on a third-party client would mean writing an **X-Token emulator that feeds directly
from the real osu! client** to obtain valid signatures. That is technically possible, but it
is **completely out of scope for a legitimate project**: it edges into impersonating the
official client against the live server, which we will not do. EMPYREAN therefore targets
`dev.ppy.sh` for online features and treats live-server score submission as out of scope.

Known production hosts (`osu.ppy.sh`, `lazer.ppy.sh`, `ppy.sh`) are still routed to the
correct official endpoint configuration if you switch to them, so login works; but
`dev.ppy.sh` is the supported default.

---

## Supported platforms

- **Windows** (mainstream desktop environments)
- **Linux — Ubuntu 24.04**

Other Linux distributions are untested. macOS, Android and iOS are out of scope. The
codebase is kept simple enough that forks can add platforms.

### Minimum requirements

EMPYREAN runs on the same engine as osu!lazer, so the bar to *run* it is essentially lazer's
baseline — what EMPYREAN changes is the performance ceiling, not the entry requirements.
These figures are derived from that baseline rather than independently benchmarked; treat
them as guidance, not a guarantee.

**Common to both platforms**

- A **64-bit** CPU and OS (there is no 32-bit build).
- A dual-core CPU (any modern CPU is comfortably enough).
- **2 GB RAM minimum**, 4 GB or more recommended (large beatmap collections and high frame
  rates use more).
- A GPU that supports **Vulkan** (strongly recommended — it is the default renderer) or, as a
  fallback, **OpenGL 3.3+**. Effectively any GPU from the last decade qualifies.
- Roughly **500 MB** of disk for the client itself, plus whatever your beatmaps need.
- Up-to-date graphics drivers.

**Linux (Ubuntu)**

- **Ubuntu 24.04** is the officially supported and tested release. Other distros may work but
  are untested.
- Vulkan drivers installed for the default renderer (e.g. `mesa-vulkan-drivers`, or your
  GPU vendor's Vulkan package).
- To run the **`Empyrean.AppImage`**: either `libfuse2` (FUSE 2) installed, or launch it with
  `./Empyrean.AppImage --appimage-extract-and-run` if FUSE is unavailable.
- An X11 or Wayland desktop session.

**Windows**

- **Windows 10 (64-bit) or newer** (Windows 11 supported).
- Up-to-date GPU drivers with Vulkan support (recommended); the OpenGL fallback works
  otherwise.
- No separate .NET install needed — the published `Empyrean.exe` is self-contained.

---

## Building

You need the **.NET 8 SDK**.

```bash
# Linux (Release) — produces a build under osu/osu.Desktop/bin/Release/net8.0
./build_linux.sh

# Windows (run on Windows)
build_windows.bat

# BOTH a Windows .exe and a Linux .AppImage from one Ubuntu 24.04 host:
./build_all.sh                 # -> dist/Empyrean.exe and dist/Empyrean.AppImage
./build_all.sh Release linux   # just one target: linux | windows | both
```

`build_all.sh` uses .NET's runtime-identifier cross-compilation (`-r win-x64` /
`-r linux-x64`) to produce a self-contained single-file `Empyrean.exe` and assembles a
portable `Empyrean.AppImage` (downloading `appimagetool` automatically) — no Windows machine
or Wine required.

### Publishing the repos

`push_all.sh` creates (if missing) and pushes the three GitHub repositories — **EMPYREAN**
(the full monorepo), **EMPYREAN-FRAMEWORK** (`osu-framework/`) and **EMPYREAN-RESOURCES**
(`osu-resources/`). It needs the GitHub CLI (`gh auth login`):

```bash
./push_all.sh "commit message"            # under your gh account, public
VISIBILITY=private ./push_all.sh "msg"     # private
GH_OWNER=myorg ./push_all.sh "msg"         # under an org
```

## Running

```bash
./run_linux.sh               # applies the Vulkan + MultiThreaded low-latency profile
./run_linux.sh --benchmark   # defers to the offline BenchmarkDotNet harness
```

When launching a published build directly, set `OSU_GRAPHICS_RENDERER=vulkan` and
`OSU_EXECUTION_MODE=MultiThreaded` yourself to get the EMPYREAN profile.

---

## Repository layout

| Path | What it is |
|------|------------|
| `osu/` | The game (osu!lazer fork). EMPYREAN code lives under `osu/osu.Game/Empyrean/`. |
| `osu-framework/` | The game framework (osu-framework fork). Engine-level changes (FPS caps, render pipeline, the BufferedContainer sharpen hook) live here. Publishable as **EMPYREAN-FRAMEWORK**. |
| `osu-resources/` | Fonts, samples, skins, textures and **shaders** (including `sh_EZHDSR.fs`). Built from source so custom shaders can be added. Publishable as **EMPYREAN-RESOURCES**. |
| `docs/` | `CHANGES.md` (dated change log) and `BENCHMARKS.md`. |
| `PROJECT.md` | The full design doctrine and phase plan. |
| `AGENT.md` | Onboarding for AI agents working on this codebase. |

---

## A note on honesty

This project tries hard not to claim performance it cannot demonstrate. Several attempted
optimizations (an in-engine framebuffer upscaler, an in-app FSR-style sharpen) were tried,
measured, found not to help, and either scrapped or defaulted off — and that history is
documented in `docs/CHANGES.md` rather than hidden. If a feature here says it improves
performance, it is because it was measured to, or because the mechanism is sound and the
caveats are stated alongside it.

---

## Credits

- Creator: **EZHD KING**
- Built on [osu!lazer](https://github.com/ppy/osu) and
  [osu-framework](https://github.com/ppy/osu-framework) by ppy Pty Ltd, used under their
  respective licences.

EMPYREAN is an independent fork and is not affiliated with or endorsed by ppy Pty Ltd.
