// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace osu.Game.Empyrean.Terminal
{
    /// <summary>
    /// Hooks the terminal needs into the live game. Implemented by the overlay so the
    /// command definitions stay free of osu! drawable/DI dependencies and remain unit testable.
    /// Every hook is a small, explicit operation — no hidden per-frame work.
    /// </summary>
    public interface ITerminalContext
    {
        // Mods
        IReadOnlyList<string> ActiveModAcronyms { get; }
        bool ToggleMod(string acronym);

        // Network (PROJECT.md §7)
        bool NetworkEnabled { get; }
        void SetNetwork(bool enabled);
        string Endpoint { get; }

        // Rendering / performance
        bool FpsDisplayVisible { get; set; }
        string FrameSyncMode { get; set; }
        IReadOnlyList<string> FrameSyncOptions { get; }

        // Audio
        double AudioOffsetMs { get; set; }

        // Diagnostics
        string PerfSnapshot();
        IEnumerable<string> RunGameplayBenchmark();

        // Reloads (safe ones only)
        void ReloadSkin();
        void ReloadMap();
    }

    /// <summary>
    /// Builds the EMPYREAN command set. Pure factory — produces a configured
    /// <see cref="TerminalEngine"/> bound to the supplied context.
    /// </summary>
    public static class TerminalCommands
    {
        public static TerminalEngine Build(ITerminalContext ctx)
        {
            var engine = new TerminalEngine();

            engine.Register(new TerminalCommand("help", "help [command]",
                "list commands or show usage for one", args =>
                {
                    if (args.Length > 0)
                    {
                        var match = engine.Commands.FirstOrDefault(c => string.Equals(c.Name, args[0], StringComparison.OrdinalIgnoreCase));
                        return match == null
                            ? new[] { $"no such command: {args[0]}" }
                            : new[] { $"{match.Name} — {match.Description}", $"usage: {match.Usage}" };
                    }

                    var lines = new List<string> { "commands:" };
                    lines.AddRange(engine.Commands.Select(c => $"  {c.Name,-12} {c.Description}"));
                    return lines;
                }, aliases: new[] { "?" }));

            engine.Register(new TerminalCommand("about", "about",
                "show creator and version", _ => EmpyreanInfo.Banner.Split('\n')));

            // ---- Mods ----------------------------------------------------------------
            engine.Register(new TerminalCommand("mod", "mod <acronym>",
                "toggle a mod by acronym (hd, hr, dt, ...)", args =>
                {
                    if (args.Length == 0)
                    {
                        var active = ctx.ActiveModAcronyms;
                        return new[] { active.Count == 0 ? "no mods active" : "active: " + string.Join(" ", active) };
                    }

                    string acr = args[0].ToUpperInvariant();
                    bool nowOn = ctx.ToggleMod(acr);
                    return new[] { $"{acr} {(nowOn ? "ON" : "OFF")}" };
                }));

            engine.Register(new TerminalCommand("mods", "mods",
                "list currently active mods", _ =>
                {
                    var active = ctx.ActiveModAcronyms;
                    return new[] { active.Count == 0 ? "no mods active" : "active: " + string.Join(" ", active) };
                }));

            // ---- Network -------------------------------------------------------------
            engine.Register(new TerminalCommand("net", "net <on|off|status>",
                "control network connectivity", args =>
                {
                    if (args.Length == 0 || args[0].Equals("status", StringComparison.OrdinalIgnoreCase))
                        return new[] { $"network: {(ctx.NetworkEnabled ? "ON" : "OFF")}  endpoint: {ctx.Endpoint}" };

                    switch (args[0].ToLowerInvariant())
                    {
                        case "on":
                            ctx.SetNetwork(true);
                            return new[] { $"network ON  ({ctx.Endpoint})" };

                        case "off":
                            ctx.SetNetwork(false);
                            return new[] { "network OFF — offline mode, gameplay unaffected" };

                        default:
                            return new[] { "usage: net <on|off|status>" };
                    }
                }));

            // ---- FPS / frame sync ----------------------------------------------------
            engine.Register(new TerminalCommand("fps", "fps [mode]",
                "set frame limiter (vsync, 2x, 4x, 8x, unlimited) or show current", args =>
                {
                    if (args.Length == 0)
                        return new[] { $"framesync: {ctx.FrameSyncMode}   options: {string.Join(", ", ctx.FrameSyncOptions)}" };

                    string req = args[0];
                    // accept "1000" / "unlimited" style requests loosely
                    if (req == "1000" || req.Equals("max", StringComparison.OrdinalIgnoreCase) || req.Equals("unlimited", StringComparison.OrdinalIgnoreCase))
                        req = ctx.FrameSyncOptions.LastOrDefault() ?? "Unlimited";

                    var matched = ctx.FrameSyncOptions.FirstOrDefault(o => o.Replace(" ", "").Equals(req.Replace(" ", ""), StringComparison.OrdinalIgnoreCase));
                    if (matched == null)
                        return new[] { $"unknown mode '{args[0]}'. options: {string.Join(", ", ctx.FrameSyncOptions)}" };

                    ctx.FrameSyncMode = matched;
                    return new[] { $"framesync -> {matched}" };
                }));

            engine.Register(new TerminalCommand("perf", "perf [on|off]",
                "toggle FPS/perf overlay or print a snapshot", args =>
                {
                    if (args.Length == 0)
                        return ctx.PerfSnapshot().Split('\n');

                    switch (args[0].ToLowerInvariant())
                    {
                        case "on":
                            ctx.FpsDisplayVisible = true;
                            return new[] { "perf overlay ON" };

                        case "off":
                            ctx.FpsDisplayVisible = false;
                            return new[] { "perf overlay OFF" };

                        default:
                            return new[] { "usage: perf [on|off]" };
                    }
                }, aliases: new[] { "show" }));

            // ---- Audio ---------------------------------------------------------------
            engine.Register(new TerminalCommand("audio", "audio latency [ms]",
                "show or set global audio offset in ms", args =>
                {
                    if (args.Length >= 2 && args[0].Equals("latency", StringComparison.OrdinalIgnoreCase)
                                         && double.TryParse(args[1], NumberStyles.Float, CultureInfo.InvariantCulture, out double ms))
                    {
                        ctx.AudioOffsetMs = ms;
                        return new[] { $"audio offset -> {ms:0} ms" };
                    }

                    return new[] { $"audio offset: {ctx.AudioOffsetMs:0} ms   (audio latency <ms> to change)" };
                }));

            // ---- Reloads -------------------------------------------------------------
            engine.Register(new TerminalCommand("reload", "reload <skin|map>",
                "reload skin or current beatmap", args =>
                {
                    if (args.Length == 0)
                        return new[] { "usage: reload <skin|map>" };

                    switch (args[0].ToLowerInvariant())
                    {
                        case "skin":
                            ctx.ReloadSkin();
                            return new[] { "skin reloaded" };

                        case "map":
                            ctx.ReloadMap();
                            return new[] { "beatmap reloaded" };

                        default:
                            return new[] { "usage: reload <skin|map>" };
                    }
                }));

            // ---- Benchmark -----------------------------------------------------------
            engine.Register(new TerminalCommand("benchmark", "benchmark gameplay",
                "run the in-client gameplay benchmark", args =>
                {
                    if (args.Length == 0 || args[0].Equals("gameplay", StringComparison.OrdinalIgnoreCase))
                        return ctx.RunGameplayBenchmark().ToList();

                    return new[] { "usage: benchmark gameplay" };
                }, aliases: new[] { "bench" }));

            engine.Register(new TerminalCommand("clear", "clear", "clear the terminal output",
                _ => new[] { "\f" }, aliases: new[] { "cls" }));

            return engine;
        }
    }
}
