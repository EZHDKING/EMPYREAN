// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using osu.Game.Empyrean.Terminal;

namespace osu.Game.Tests.NonVisual.Empyrean
{
    /// <summary>
    /// Tests for the EMPYREAN terminal engine and command set. These are deliberately pure
    /// (no game host, no drawables) so they run instantly and protect command-parsing
    /// behaviour per PROJECT.md §21. The engine is exercised through a fake context.
    /// </summary>
    [TestFixture]
    public class TerminalEngineTest
    {
        private FakeContext context = null!;
        private TerminalEngine engine = null!;

        [SetUp]
        public void SetUp()
        {
            context = new FakeContext();
            engine = TerminalCommands.Build(context);
        }

        [Test]
        public void UnknownCommandIsReportedNotThrown()
        {
            var output = engine.Run("definitely-not-a-command");
            Assert.That(output.Single(), Does.Contain("unknown command"));
        }

        [Test]
        public void EmptyInputProducesNoOutputAndNoHistory()
        {
            Assert.That(engine.Run("   "), Is.Empty);
            Assert.That(engine.History, Is.Empty);
        }

        [Test]
        public void HelpListsAllCommands()
        {
            var output = engine.Run("help");
            Assert.That(output.First(), Is.EqualTo("commands:"));
            // every registered command should appear in the listing
            foreach (var c in engine.Commands)
                Assert.That(output.Any(l => l.Contains(c.Name)), $"help missing {c.Name}");
        }

        [Test]
        public void ModToggleFlipsState()
        {
            var on = engine.Run("mod hd");
            Assert.That(on.Single(), Is.EqualTo("HD ON"));
            Assert.That(context.Mods, Does.Contain("HD"));

            var off = engine.Run("mod hd");
            Assert.That(off.Single(), Is.EqualTo("HD OFF"));
            Assert.That(context.Mods, Does.Not.Contain("HD"));
        }

        [Test]
        public void NetOnOffUpdatesContext()
        {
            engine.Run("net off");
            Assert.That(context.NetworkEnabled, Is.False);

            engine.Run("net on");
            Assert.That(context.NetworkEnabled, Is.True);
        }

        [Test]
        public void NetStatusReportsEndpoint()
        {
            var output = engine.Run("net status");
            Assert.That(output.Single(), Does.Contain("dev.ppy.sh"));
        }

        [Test]
        public void FpsAcceptsUnlimitedAlias()
        {
            engine.Run("fps 1000");
            Assert.That(context.FrameSyncMode, Is.EqualTo(context.FrameSyncOptions.Last()));
        }

        [Test]
        public void FpsRejectsUnknownMode()
        {
            var output = engine.Run("fps banana");
            Assert.That(output.Single(), Does.Contain("unknown mode"));
        }

        [Test]
        public void AudioLatencySetAndRead()
        {
            engine.Run("audio latency 12");
            Assert.That(context.AudioOffsetMs, Is.EqualTo(12));

            var output = engine.Run("audio");
            Assert.That(output.Single(), Does.Contain("12"));
        }

        [Test]
        public void PerfToggleControlsOverlay()
        {
            engine.Run("perf on");
            Assert.That(context.FpsDisplayVisible, Is.True);
            engine.Run("perf off");
            Assert.That(context.FpsDisplayVisible, Is.False);
        }

        [Test]
        public void AutocompletePrefixMatches()
        {
            var matches = engine.Complete("m");
            Assert.That(matches, Does.Contain("mod"));
            Assert.That(matches, Does.Contain("mods"));
            Assert.That(matches, Does.Not.Contain("net"));
        }

        [Test]
        public void HistoryRecordsExecutedLines()
        {
            engine.Run("help");
            engine.Run("about");
            Assert.That(engine.History, Is.EqualTo(new[] { "help", "about" }));
        }

        [Test]
        public void BenchmarkDoesNotFabricateNumbers()
        {
            // PROJECT.md §3.3 / §20.4: never report unverified in-session performance numbers.
            var output = engine.Run("benchmark gameplay");
            Assert.That(output.Any(l => l.Contains("harness")), "should defer to the offline harness");
            Assert.That(output.All(l => !l.Contains("FPS:")), "must not print fabricated FPS figures");
        }

        /// <summary>In-memory fake implementing every terminal hook.</summary>
        private class FakeContext : ITerminalContext
        {
            private readonly HashSet<string> mods = new HashSet<string>();
            public IReadOnlyList<string> Mods => mods.ToList();

            public IReadOnlyList<string> ActiveModAcronyms => mods.ToList();

            public bool ToggleMod(string acronym)
            {
                if (mods.Contains(acronym)) { mods.Remove(acronym); return false; }
                mods.Add(acronym); return true;
            }

            public bool NetworkEnabled { get; private set; } = true;
            public void SetNetwork(bool enabled) => NetworkEnabled = enabled;
            public string Endpoint => "https://dev.ppy.sh";

            public bool FpsDisplayVisible { get; set; }

            private string frameSync = "VSync";
            public string FrameSyncMode { get => frameSync; set => frameSync = value; }
            public IReadOnlyList<string> FrameSyncOptions => new[] { "VSync", "2x refresh rate", "4x refresh rate", "8x refresh rate", "Basically unlimited" };

            public double AudioOffsetMs { get; set; }

            public string PerfSnapshot() => "framesync : " + frameSync;
            public IEnumerable<string> RunGameplayBenchmark() => new[] { "run the offline harness instead", "tools/benchmark" };

            public void ReloadSkin() { }
            public void ReloadMap() { }
        }
    }
}
