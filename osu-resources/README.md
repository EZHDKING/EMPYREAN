# EMPYREAN-RESOURCES

**A fork of [osu-resources](https://github.com/ppy/osu-resources) carrying the shared assets
for the EMPYREAN client — including EMPYREAN's custom shaders.**

> Part of the EMPYREAN project by **EZHD KING**. EMPYREAN-RESOURCES is the asset package;
> EMPYREAN (the game) consumes it, and it is built against EMPYREAN-FRAMEWORK.

osu-resources is the shared resource library for osu!: fonts, audio samples, default skins,
textures, tracks and **shaders**, all shipped as embedded resources in a single assembly
(`osu.Game.Resources`, package id `ppy.osu.Game.Resources`). The game loads them at runtime
through the framework's resource stores.

EMPYREAN builds this from **source** (rather than consuming the prebuilt NuGet package) for
one key reason: **so custom shaders can be added.** The prebuilt package cannot be extended;
the source project can.

---

## What this fork changes vs upstream osu-resources

### Built from source, targeting net8.0
- The project targets **net8.0** and references **EMPYREAN-FRAMEWORK** (the local
  `osu.Framework` project) instead of the old `ppy.osu.Framework` NuGet package. This keeps
  the shader format and framework APIs aligned with the engine the game actually runs on.
- The consuming game references this project directly (`ProjectReference`) instead of the
  `ppy.osu.Game.Resources` package.

### New shader: `Shaders/sh_EZHDSR.fs`
EMPYREAN's **EZHDSR** sharpening shader — a CAS/FSR1-style contrast-adaptive spatial sharpen
used as the blit shader of a `BufferedContainer` (see EMPYREAN-FRAMEWORK's
`GetCustomTextureShader` hook). Design points:
- It is a structural drop-in for the framework's default texture shader (`sh_Texture.fs`):
  same includes (`sh_Utils.h`, `sh_Masking.h`, `sh_TextureWrapping.h`), `v_TexCoord` at
  location 2, output through `getRoundedColor(...)`. Masking, corner-rounding and wrapping
  therefore still work — it only adds a sharpening step on top of the centre sample.
- It is **uniform-free**: the framebuffer blit path does not populate custom uniform blocks,
  so strength is a constant and the texel step is derived from `textureSize()`.
- 5-tap kernel (centre + 4 edge neighbours), contrast-weighted, clamped to the local
  neighbourhood min/max to suppress ringing/halos. The cheapest kernel that still gives
  FSR1-like edge recovery.

> Honest note for downstream users: EZHDSR sharpens *inside* the app. If your display does
> the final upscale (e.g. you render at a lower real resolution and the monitor scales up),
> that hardware upscale happens after the app and re-blurs the sharpening, and the extra
> render pass costs frames. EZHDSR is most useful when the app itself owns the final image
> size. In EMPYREAN it ships **off by default** for this reason.

---

## How shaders are embedded

The project file embeds everything under each asset directory:

```xml
<EmbeddedResource Include="Shaders\**\*" />
```

So adding a shader is just dropping a file into `osu.Game.Resources/Shaders/`. The
framework's `ShaderManager` resolves a logical name (e.g. `"EZHDSR"`) to the file
`sh_EZHDSR.fs` (it adds the `sh_` prefix and `.fs`/`.vs` extension). Fragment shaders that
replace the default texture shader must follow the structure described above.

Directory overview:

| Directory | Contents |
|-----------|----------|
| `Fonts/` | Inter, Torus, Torus-Alternate, Noto, etc. |
| `Samples/` | UI and gameplay audio samples |
| `Skins/` | Default skins |
| `Textures/` | Shared textures |
| `Tracks/` | Built-in audio tracks |
| `Shaders/` | GLSL shaders (incl. `sh_EZHDSR.fs`) |
| `Beatmaps/` | Bundled beatmaps |

---

## Building

```bash
dotnet build osu.Game.Resources/osu.Game.Resources.csproj -c Release
```

Requirements: .NET 8 SDK, and the EMPYREAN-FRAMEWORK project available at the referenced
relative path (`../../osu-framework/osu.Framework/osu.Framework.csproj` in the EMPYREAN
layout). Adjust the `ProjectReference` if you relocate it.

---

## Relationship to the other EMPYREAN repos

- **EMPYREAN-FRAMEWORK** — the engine; this project references it so shader/resource formats
  match.
- **EMPYREAN** (the game) — references this project directly to pick up the custom shaders
  and assets.

---

## Licence & credits

Forked from [osu-resources](https://github.com/ppy/osu-resources) by ppy Pty Ltd, used under
its licence (see `LICENCE.md`). Bundled fonts and assets retain their own licences.
EMPYREAN-RESOURCES is an independent fork and is not affiliated with or endorsed by ppy Pty
Ltd. Fork maintainer: **EZHD KING**.
