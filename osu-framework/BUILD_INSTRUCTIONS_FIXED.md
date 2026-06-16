# Build instructions for the fixed osu-framework archive

## What was changed
- Raised the remaining realtime headless frame cap from 1000 to 10000 FPS in `osu.Framework/Platform/HeadlessGameHost.cs`.
- The framework's normal active-thread ceiling is already set to 10000 FPS in this snapshot.

## Build the framework DLL

From the repository root:

```bash
dotnet restore
dotnet build osu.Framework/osu.Framework.csproj -c Release
```

The framework DLL will be here:

```text
osu.Framework/bin/Release/net8.0/osu.Framework.dll
```

## Optional: build sample game

```bash
dotnet build SampleGame.Desktop/SampleGame.Desktop.csproj -c Release
```

The sample game output will be under:

```text
SampleGame.Desktop/bin/Release/net8.0/
```

## Optional: run tests

```bash
dotnet test osu.Framework.Tests/osu.Framework.Tests.csproj -c Release
```

## Notes
- `FrameSync.Unlimited` is still available in the framework, and the runtime cap is controlled by `AllowBenchmarkUnlimitedFrames` in `GameHost`.
- No project file changes were made; this is a source-only patch.
