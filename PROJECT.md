# EMPYREAN — AI Agent Master Guide

## 0. Purpose of this document

This document is the canonical operating guide for any AI agent working on **EMPYREAN**.

EMPYREAN is a deliberately stripped-down, high-performance fork of an osu!lazer-based client, designed around one priority only:

**maximum gameplay performance for top-level play, with the smallest possible amount of unnecessary runtime cost.**

Every implementation decision must answer a single question:

> Does this improve raw gameplay performance, input latency, frametime stability, or reliability for competitive play?

If the answer is not clearly yes, the feature is either removed, disabled, deferred, or compiled out.

---

## 1. Core mission

### 1.1 Primary objective
Build the most performance-extreme, competition-first version of the client possible while retaining the minimum feature set required for the game to function correctly.

### 1.2 Secondary objective
Reduce complexity everywhere:
- fewer runtime systems
- fewer background tasks
- fewer allocations
- fewer draw calls
- fewer dependencies
- fewer layers of abstraction when they do not help performance

### 1.3 Tertiary objective
Keep the codebase maintainable enough that future contributors can safely optimize, profile, and extend it without reintroducing wasted overhead.

---

## 2. Identity of the project

EMPYREAN is not a general-purpose rhythm game client.
It is a **competitive performance fork**.

It should feel like:
- a barebones Windows 95-era desktop application
- a minimal command-driven tool
- a game client that spends nearly all of its budget on gameplay

The visual identity must remain intentionally primitive and utility-focused.

### 2.1 Required attribution
The project must visibly credit the creator:
- **Creator: EZHD KING**

This credit should appear in:
- about / credits screen
- README
- any launch/about dialog if present
- optional terminal banner

---

## 3. Product philosophy

### 3.1 Absolute priorities
From highest to lowest:
1. Gameplay timing accuracy
2. Input latency reduction
3. Frametime consistency
4. Rendering efficiency
5. Memory allocation reduction
6. Load-time reduction
7. Network reliability
8. Basic usability
9. Visual polish

### 3.2 Non-goals
EMPYREAN must not chase these goals unless they directly improve gameplay:
- visual richness
- animation-heavy UX
- social network features
- content discovery features
- cinematic presentation
- decorative backgrounds
- music visualizers
- splash screens
- seasonal themes
- news feeds
- update popups
- onboarding tutorials
- expansive profile systems
- expensive live UI effects

### 3.3 Performance truth
The agent must not assume that visually removing elements automatically improves latency.
Any claimed performance gain must be:
- measurable
- profiled
- reproducible
- justified by data

If a change is intuitive but not measured, it is only a hypothesis.

---

## 4. Scope of the fork

EMPYREAN should retain only the systems required for a practical competitive client.

### 4.1 Keep
- application boot and shutdown
- settings storage
- login / network connection controls
- beatmap discovery / selection
- gameplay
- results display
- basic replay playback and review tools if they do not add unnecessary overhead
- local assets and skins
- minimal sound and input handling
- minimal update / version handling only if needed for compatibility

### 4.2 Optional, only if cost is justified
- multiplayer
- online leaderboards
- score submission
- beatmap download support
- basic cloud synchronization

### 4.3 Remove or disable by default
- intro animations
- menu transitions
- background videos
- moving background effects
- shader-heavy decorations
- rich social UI
- complex profile pages
- news panels
- in-client web content unless strictly necessary
- excessive telemetry
- automatic background update activity
- error surfaces that are decorative rather than functional
- unnecessary overlays
- debug visualizers in production
- any “pleasant” feature that competes with runtime budget

---

## 5. UI and visual design rules

### 5.1 Required visual style
The client should resemble a **minimal Windows 95 era utility**:
- flat or nearly flat surfaces
- low-complexity layout
- basic borders
- plain buttons
- simple listboxes
- minimal icon usage
- no translucency
- no blur
- no parallax
- no animated gradients
- no shadows unless they are effectively free and justified

### 5.2 Design principle
The UI is not the product.
The gameplay is the product.

UI should exist only to:
- launch the game
- select maps
- change relevant settings
- configure latency/performance options
- manage accounts/network state
- inspect results
- invoke terminal commands

### 5.3 Rendering guidance
Prefer:
- static controls
- simple text
- reusable assets
- tightly bounded layout invalidation
- reduced overdraw
- minimal textures

Avoid:
- constantly changing visuals
- large animated containers
- expensive clipping chains
- redundant masking
- deeply nested drawable hierarchies

---

## 6. Terminal / command console

EMPYREAN should include a simple MS-DOS-era terminal interface that can be used for fast power-user actions.

### 6.1 Purpose
The terminal is for:
- toggling mods
- adjusting performance settings
- switching network state
- changing rendering options
- reloading assets/config where safe
- running diagnostics and benchmarks

### 6.2 Required qualities
- extremely light rendering cost
- instant input responsiveness
- command autocomplete if it does not meaningfully harm performance
- command history
- readable monospace display
- simple output formatting

### 6.3 Example commands
- `mod hd`
- `mod hr`
- `mod dt`
- `net on`
- `net off`
- `fps 1000`
- `audio latency`
- `benchmark gameplay`
- `reload skin`
- `reload map`
- `show perf`

### 6.4 Terminal philosophy
The terminal is not a toy.
It is a fast operational interface for high-level control.

---

## 7. Network policy

### 7.1 Default state
Network connectivity should be **ON by default**.

### 7.2 Default connection target
The default configured endpoint should point to the development instance:
- `dev.ppy.sh`

### 7.3 User control
Users must be able to disable networking cleanly through settings without breaking core offline gameplay.

### 7.4 Offline guarantees
When networking is disabled:
- core gameplay must still function
- local beatmap selection must still function
- settings must still work
- no background connection attempts should continue
- no UI should depend on network availability for basic play

### 7.5 Network performance rules
- no unnecessary polling
- no hidden background sync loops
- no forced reconnect spam
- no expensive retries without backoff
- no networking work on critical gameplay threads unless unavoidable

---

## 8. Supported platforms

EMPYREAN supports only:
- **Windows**
- **Linux (Ubuntu 24.04)**

All other platforms are out of scope unless explicitly forked by another maintainer.

### 8.1 Build scripts required
Provide and maintain:
- `build_linux.sh`
- `run_linux.sh`
- `install_linux.sh`
- `build_windows.bat`
- `run_windows.bat`
- `install_windows.bat`

### 8.2 Platform assumptions
The agent may assume:
- Ubuntu 24.04 on Linux
- mainstream Windows desktop environments

Do not introduce platform-specific complexity for unsupported environments.

---

## 9. Repository structure expectations

A clean structure is required.

Suggested top-level layout:

```text
/README.md
/AGENT.md
/build_linux.sh
/run_linux.sh
/install_linux.sh
/build_windows.bat
/run_windows.bat
/install_windows.bat
/src
/tests
/tools
/docs
/assets
```

The agent may refine this, but the structure must stay simple and obvious.

---

## 10. Performance doctrine

### 10.1 The golden rule
Every frame matters.
Every allocation matters.
Every extra traversal matters.
Every hidden task matters.

### 10.2 Optimization targets
The agent should aggressively optimize around:
- input handling latency
- render submission overhead
- CPU time per frame
- memory churn and GC pressure
- asset loading cost
- shader cost
- layout invalidation cost
- timing precision
- audio playback timing

### 10.3 Optimization methods the agent should prefer
- pooling
- caching
- precomputation
- structural simplification
- fewer drawables
- fewer allocations
- fewer virtual hops where practical
- reduced per-frame work
- minimized per-object bookkeeping
- faster hot paths

### 10.4 Optimization methods the agent should avoid unless proven necessary
- adding large abstractions for convenience
- speculative architecture changes
- feature-rich “general solutions” that increase complexity
- background worker proliferation
- clever-but-obscure code that future maintainers cannot reason about

---

## 11. Gameplay-first rendering guidance

### 11.1 Gameplay scene priority
Gameplay rendering should be the cleanest and fastest scene in the application.

### 11.2 During gameplay
The agent should aim to minimize:
- draw calls
- state changes
- texture switches
- invalidation cascades
- expensive UI updates
- expensive logging
- unnecessary composition work

### 11.3 During gameplay, strongly prefer
- static HUD elements
- small and predictable overlay updates
- preloaded assets
- simple hit feedback
- minimal post-processing

### 11.4 During gameplay, avoid
- background video playback
- decorative particles
- animated menus in the render tree
- live chat overlays unless truly required
- anything that could steal CPU/GPU budget from note timing and frame stability

---

## 12. Audio and input priorities

### 12.1 Audio timing
Audio timing is critical.
The agent must treat audio latency and synchronization as a first-class system.

### 12.2 Input handling
Input handling should be:
- fast
- deterministic
- simple
- low allocation
- minimally layered

### 12.3 Input latency work
The agent should inspect whether any changes reduce:
- input-to-action delay
- buffering overhead
- thread handoff overhead
- scheduling jitter

### 12.4 Audio and input must not be burdened by UI concerns
The gameplay path must be kept free from unnecessary UI or network work.

---

## 13. Framework/engine optimization rules

EMPYREAN should be treated as a chance to optimize the framework in service of a single workload: high-speed rhythm gameplay.

### 13.1 Hot path principles
In hot paths:
- avoid allocations
- avoid unnecessary LINQ / iterator overhead
- avoid repeated property traversal where cached values are sufficient
- avoid repeated layout recalculation when state is stable
- avoid redundant invalidations
- avoid expensive logging

### 13.2 Drawable hierarchy
Keep hierarchies shallow where possible.
Large, deeply nested drawable graphs should be treated as suspicious unless they clearly pay for themselves.

### 13.3 Update loops
Per-frame updates should be:
- explicit
- minimal
- narrowly scoped
- easy to reason about

The agent should ask: “Does this need to run every frame?”

### 13.4 Caching strategy
Cache aggressively when the cached data:
- is stable
- is cheap to invalidate safely
- reduces repeated computation

Do not cache blindly if it complicates correctness.

### 13.5 Object lifetime
Prefer object reuse for frequently created transient items.
The goal is to reduce pressure on memory allocators and garbage collection.

---

## 14. Logging and diagnostics policy

### 14.1 Production logging
Keep production logging minimal and purposeful.

### 14.2 What should be logged
- fatal failures
- configuration load issues
- network connection state changes
- benchmark results
- important timing anomalies
- recoverable but meaningful runtime errors

### 14.3 What should not be logged excessively
- per-frame spam
- noisy debug traces in release builds
- repetitive informational logs that do not help diagnosis

### 14.4 Diagnostics philosophy
Diagnostics should help the developer measure and fix performance issues without becoming performance issues themselves.

---

## 15. Error handling philosophy

Errors should be:
- clear
- actionable
- non-blocking when possible
- minimal in presentation cost

Do not use error systems that are decorative, animated, or heavy.

### 15.1 Preferred error behavior
- small text message
- simple dialog
- optional terminal output
- recoverable state when possible

### 15.2 Avoid
- elaborate animated error screens
- expensive stack-trace rendering during gameplay
- intrusive popups that steal focus unless truly necessary

---

## 16. Settings design

### 16.1 Required settings philosophy
Settings should be few, clear, and gameplay-relevant.

### 16.2 Performance-related settings to expose
- FPS cap / frame limiter behavior
- audio latency tuning
- renderer selection if applicable
- network on/off
- background / storyboard / video toggles
- low-latency mode
- terminal availability
- debug overlays

### 16.3 Settings UX
Settings UI should be simple and fast, not expansive.
Group controls by:
- gameplay
- audio
- video/rendering
- network
- terminal/tools
- misc

### 16.4 Default principle
Defaults should favor competitive play and predictable performance.

---

## 17. Visual content policy

### 17.1 By default, disable or strip
- animated backgrounds
- beatmap videos
- storyboards
- particle-heavy effects
- ornamental transitions
- splash animations

### 17.2 Optional toggles
If a visual feature remains for compatibility, it should be:
- off by default
- easy to disable
- isolated so it does not touch the gameplay hot path

### 17.3 Low-cost compatibility rule
If a feature is retained only for compatibility, it should be implemented in the cheapest practical form.

---

## 18. Update policy

### 18.1 No noisy updates
Do not add update prompts or update banners that interrupt the user.

### 18.2 Acceptable update behavior
- silent version checks only if necessary
- manual update flows
- explicit user-invoked update logic

### 18.3 Never do in the foreground
- forced popups during gameplay
- animated changelog presentations
- background download work that impacts timing without consent

---

## 19. Compatibility policy

### 19.1 Compatibility with upstream concepts
Where practical, preserve compatibility with the underlying osu!lazer ecosystem in a lightweight way.

### 19.2 Compatibility is subordinate to performance
If compatibility increases cost significantly, prefer the leaner solution.

### 19.3 Fork philosophy
This project is allowed to diverge when divergence is what makes the client better for competitive play.

---

## 20. Benchmarks and validation

### 20.1 Every meaningful performance change must be validated
The agent must compare before and after using realistic scenarios.

### 20.2 Benchmark categories
- startup time
- song select responsiveness
- beatmap load time
- gameplay FPS stability
- input-to-judgement latency
- memory usage under sustained play
- GC behavior during gameplay
- audio timing consistency
- UI responsiveness under stress

### 20.3 Benchmark discipline
The agent must record:
- what changed
- why it changed
- the expected gain
- the measured gain
- whether the gain was real or negligible

### 20.4 No unverified performance claims
Do not claim a change improved performance unless the data supports it.

---

## 21. Testing policy

### 21.1 Required test types
- unit tests for logic where practical
- integration tests for startup / settings / networking paths
- gameplay smoke tests
- regression tests for performance-sensitive code where practical
- build verification on supported platforms

### 21.2 Test philosophy
Tests should protect:
- timing correctness
- configuration stability
- loading correctness
- command terminal behavior
- platform build reproducibility

### 21.3 Performance-sensitive tests
The agent should avoid introducing tests that are themselves so expensive that they make development slow and discourage iteration.

---

## 22. Implementation priorities for the agent

The agent should generally work in this order:

### Phase 1 — Skeleton and constraints
- establish repository structure
- add build/run/install scripts
- add README and AGENT docs
- define platform targets
- define performance policy

### Phase 2 — UI stripping
- simplify main menus
- remove unnecessary animations
- replace rich interfaces with simple controls
- add Windows 95-like visual language
- add terminal interface

### Phase 3 — Core gameplay path
- verify gameplay works with stripped UI
- reduce allocations
- reduce draw overhead
- simplify scene switching
- improve load timing

### Phase 4 — Network controls
- ensure network is optional
- default to ON
- route to development instance as configured
- ensure offline mode is safe and complete

### Phase 5 — Engine/framework optimization
- profile hot paths
- reduce invalidations
- reduce allocations
- pool transient objects
- simplify rendering path
- improve timing precision

### Phase 6 — Competitive polish
- ensure stable settings
- ensure terminal commands are robust
- ensure benchmarking tools are available
- ensure release mode is clean

---

## 23. Coding style and engineering discipline

### 23.1 General style
Code should be:
- explicit
- easy to profile
- easy to reason about
- focused on fast paths
- not over-abstracted

### 23.2 Comments
Use comments for:
- why a performance tradeoff exists
- why a subsystem was removed or simplified
- why a hot path is written in an unusual way

Do not use comments to hide confused code.

### 23.3 Refactoring rule
Refactor only when it clarifies performance or reduces complexity.

---

## 24. Forbidden behaviors for the agent

The agent must not:
- add features because they are trendy
- preserve decorative systems out of habit
- introduce background work without clear need
- increase abstraction without measurable benefit
- optimize one subsystem by damaging gameplay timing elsewhere
- assume a visual simplification is automatically a performance optimization
- create sprawling, hard-to-maintain code paths
- make the project depend on expensive runtime services for core play

---

## 25. Deliverables expected from the agent

The agent should produce:
- a working stripped client
- build scripts for Linux and Windows
- run scripts for Linux and Windows
- install scripts for Linux and Windows
- a clear README
- this AGENT guide kept in sync with the codebase
- a small but meaningful benchmark workflow
- a terminal interface for power-user control
- performance-oriented defaults

---

## 26. README requirements
The README must include:
- project vision
- creator credit: EZHD KING
- supported platforms
- build instructions
- run instructions
- install instructions
- feature philosophy
- performance goals
- configuration overview
- terminal usage examples
- benchmark philosophy
- roadmap and future improvements

It should be long enough to be useful, but not bloated with marketing text.

---

## 27. Roadmap guidance

The roadmap should emphasize:
- measurable performance improvements
- minimal but useful tooling
- safer profiling infrastructure
- clearer defaults
- stronger competitive reliability

Possible future improvements include:
- better frame pacing analysis
- more granular rendering diagnostics
- safer object pooling systems
- lightweight tournament-oriented modes
- improved local benchmark tooling
- optional reduced-feature gameplay profiles

These should only be pursued if they demonstrably improve the client.

---

## 28. Final operating principle

EMPYREAN exists for one reason:

> to provide the cleanest, leanest, fastest possible gameplay experience for top osu! players, with every nonessential feature treated as expendable unless proven useful.

When in doubt, choose the path that:
- does less work
- allocates less memory
- draws less
- blocks less
- surprises less
- measures better

That is the project.

