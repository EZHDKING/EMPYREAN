# EMPYREAN benchmarks

This is the canonical log of EMPYREAN performance changes. Per `PROJECT.md` §3.3 and §20.4,
**no performance claim is valid unless it appears here with measured data.** Intuition is a
hypothesis; only a measured, reproducible run is evidence.

Run benchmarks with:

```bash
./benchmark.sh                       # Linux, interactive
./benchmark.sh --filter '*HitObject*'
benchmark.bat                        # Windows
```

Results are written under `osu/BenchmarkDotNet.Artifacts/`.

## How to record an entry

Copy the template below for every meaningful change. Be honest about negligible or
negative results — those are just as valuable as wins, and hiding them is forbidden.

```
### <date> — <short title>
- Change: <what changed, in one or two sentences>
- Why: <which priority it targets: timing / latency / frametime / overdraw / memory>
- Scenario: <benchmark name + machine: CPU, GPU, OS, refresh rate>
- Expected: <the hypothesis before measuring>
- Measured: <the actual numbers, before -> after>
- Verdict: <real win / negligible / regression — and the decision made>
```

---

## Pending validation

These changes are implemented and reasoned about, but their gameplay-path GPU impact has
**not yet been measured on real hardware**. They must be benchmarked (frametime + GC under
sustained play, ideally on an RTX card at high refresh) before any numeric claim is made.

### Flat gameplay rendering (`EmpyreanFlatGameplay`, default ON)
- Change: `MainCirclePiece` flat path draws 3 cheap elements (solid masked body + ring +
  combo number) plus one non-additive hit flash, instead of the upstream 6-layer stack
  (glow + disc texture + animated `TrianglesPiece` + kiai flash + ring + flash + explode +
  number).
- Why: removes per-frame triangle animation and several additive overdraw/blend-state
  changes from the gameplay hot path; fewer masked passes per object.
- Expected: lower CPU update cost (no triangle motion), lower GPU overdraw, especially at
  high object counts / high refresh. Magnitude unknown until measured.
- Measured: **TODO** — needs a sustained-gameplay frametime capture, flat ON vs OFF.
- Verdict: **unverified**. Do not cite an FPS number until this row is filled in.

### Renderer profile (Vulkan + MultiThreaded via run scripts)
- Change: run scripts hint `OSU_GRAPHICS_RENDERER=vulkan`, `OSU_EXECUTION_MODE=MultiThreaded`.
- Expected: lower submission cost on NVIDIA RTX vs OpenGL; lower input latency vs single-thread.
- Measured: **TODO** — compare frametime + input latency across renderer/threading combos.
- Verdict: **unverified**.
