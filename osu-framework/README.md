# EMPYREAN-FRAMEWORK

**A fork of [osu-framework](https://github.com/ppy/osu-framework) tuned for performance, with
the changes that power the EMPYREAN client.**

> Part of the EMPYREAN project by **EZHD KING**. EMPYREAN-FRAMEWORK is the engine;
> EMPYREAN (the game) and EMPYREAN-RESOURCES build on top of it.

osu-framework is a general-purpose, batteries-included 2D game framework in C#: a scene
graph of `Drawable`s, a multithreaded game loop (separate input/update/draw/audio threads),
a Veldrid-based renderer (Vulkan/Direct3D/Metal/OpenGL backends), dependency injection,
bindables, input handling, audio, and a full UI toolkit. You can build a complete game with
it — the upstream project ships templates for exactly that.

This fork keeps all of that and adds targeted engine changes for high-frame-rate, low-latency
games. It is a drop-in replacement for the upstream framework for projects that want those
characteristics.

---

## What this fork changes vs upstream osu-framework

All changes are additive or are single-value tuning; the public API is unchanged unless
noted, so existing osu-framework code builds against it.

### Uncapped frame rate
Upstream pins thread rates to a "sane" ceiling. This fork removes the artificial cap so the
loop runs as fast as the hardware allows:
- `GameThread.DEFAULT_ACTIVE_HZ` raised to effectively unlimited. This value also feeds the
  internal `maximum_sane_fps` clamp (in `GameHost.updateFrameSyncMode`) and the
  multithreaded main-thread rate in `ThreadRunner`, so raising it neutralizes all three.
- `ThrottledFrameClock.MaximumUpdateHz` raised so the clock takes its no-sleep path when
  unlimited and never throttles between frames.
- Inactive (unfocused) Hz stays low so a backgrounded window doesn't spin the CPU.

Net effect: frame rate is bounded only by how fast frames actually complete. (Past the
display refresh and input poll rate, extra frames have little practical value — this just
removes the floor.)

### Render pipeline tuning for flat, high-fill-rate rendering
- App-wide anti-aliasing blend bands removed (`CompositeDrawable.maskingSmoothness` lowered
  to a hair above zero — zero is illegal because the shader divides by it). This gives hard
  pixel edges and saves per-pixel blend cost; ideal for a flat visual style.
- `sh_FastCircle.fs` rewritten from `highp` to `mediump` circle-distance math with a hard
  (non-AA) edge when the blend range is zero.
- VRAM upload throughput raised in `Renderer.cs` (`MaxTexturesUploadedPerFrame`,
  `MaxPixelsUploadedPerFrame`, full-queue drain) to front-load uploads.

### A post-process shader hook on BufferedContainer
`BufferedContainer` gained:
```csharp
protected virtual IShader GetCustomTextureShader(ShaderManager shaders) => null;
```
A subclass can return a custom shader to draw the captured framebuffer to the screen (for
post-process effects like sharpening). Returning null keeps the default texture shader, so
behaviour is unchanged for everyone else. EMPYREAN's `EmpyreanSharpenContainer` uses this to
run a CAS/FSR1-style sharpen on the blit. Note: the framebuffer blit path does not populate
custom uniform blocks, so a shader used here must be uniform-free.

---

## Building a game on this framework

This fork is used exactly like upstream osu-framework. The upstream documentation and
tutorials apply unchanged. In brief:

1. Reference `osu.Framework` (this repo's `osu.Framework/osu.Framework.csproj`) as a project
   reference, or package it.
2. Subclass `osu.Framework.Game`, build a scene graph of `Drawable`s, and host it with an
   `osu.Framework.Platform` game host (`Host.GetSuitableDesktopHost(...)`).
3. Use the multithreaded execution mode and the Vulkan renderer for the lowest input latency
   on modern desktops (set via env: `OSU_GRAPHICS_RENDERER=vulkan`,
   `OSU_EXECUTION_MODE=MultiThreaded`, or the equivalent `FrameworkConfig` settings).

The upstream templates (`osu.Framework.Templates/`) — flappy-bird and empty-game starters —
work against this fork and are the fastest way to start a new game.

### Requirements
- .NET 8 SDK
- A GPU/driver supporting one of the Veldrid backends (Vulkan recommended)

### Build
```bash
dotnet build osu.Framework/osu.Framework.csproj -c Release
```

---

## Renderer note (Veldrid)

The renderer is **Veldrid** (`ppy.Veldrid`), a cross-platform graphics abstraction. The
framework records draw calls and forwards them to Veldrid, which translates to Vulkan (or
Direct3D/Metal/OpenGL). The actual GPU command submission lives inside the Veldrid package,
not in this repo. "Optimizing the Vulkan renderer" therefore means forking Veldrid itself —
the framework side is the glue that calls it. For most games the framework-level batching,
masking and upload behaviour matter more than the backend.

---

## Relationship to the other EMPYREAN repos

- **EMPYREAN** (the game) — a fork of osu!lazer that references this framework.
- **EMPYREAN-RESOURCES** — fonts, samples, skins, textures and shaders, including the
  EZHDSR sharpen shader; references this framework so shader/resource formats line up.

---

## Licence & credits

Forked from [osu-framework](https://github.com/ppy/osu-framework) by ppy Pty Ltd, used under
its licence (see `LICENCE`). EMPYREAN-FRAMEWORK is an independent fork and is not affiliated
with or endorsed by ppy Pty Ltd. Fork maintainer: **EZHD KING**.
