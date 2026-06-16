// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Configuration;
using osu.Framework.Extensions;
using osu.Framework.Graphics;
using osu.Game.Configuration;
using osu.Game.Empyrean.Terminal;
using osu.Game.Online.API;
using osu.Game.Rulesets;
using osu.Game.Rulesets.Mods;

namespace osu.Game.Empyrean
{
    /// <summary>
    /// Live bridge between the decoupled <see cref="TerminalEngine"/> and the running game.
    ///
    /// Every dependency is resolved defensively (canBeNull) and every method guards against
    /// missing dependencies. This is deliberate: if this component ever threw during load, it
    /// would break osu!'s async component-load chain and silently take the terminal overlay
    /// (and anything loaded after it) down with it. It must never throw at construction/BDL.
    /// </summary>
    public partial class GameTerminalContext : Component, ITerminalContext
    {
        [Resolved(canBeNull: true)]
        private OsuConfigManager localConfig { get; set; }

        [Resolved(canBeNull: true)]
        private FrameworkConfigManager frameworkConfig { get; set; }

        [Resolved(canBeNull: true)]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<IReadOnlyList<Mod>> selectedMods { get; set; }

        [Resolved(canBeNull: true)]
        private Bindable<RulesetInfo> ruleset { get; set; }

        private bool networkEnabled = true;

        // ---- mods ----------------------------------------------------------------
        public IReadOnlyList<string> ActiveModAcronyms =>
            selectedMods?.Value?.Select(m => m.Acronym).ToList() ?? new List<string>();

        public bool ToggleMod(string acronym)
        {
            if (selectedMods == null || ruleset?.Value == null)
                throw new InvalidOperationException("mods unavailable here");

            var instance = ruleset.Value.CreateInstance();
            var all = instance.CreateAllMods().ToList();
            var match = all.FirstOrDefault(m => string.Equals(m.Acronym, acronym, StringComparison.OrdinalIgnoreCase));
            if (match == null)
                throw new ArgumentException($"unknown mod '{acronym}' for {ruleset.Value.Name}");

            var current = selectedMods.Value.ToList();
            int idx = current.FindIndex(m => m.Acronym == match.Acronym);

            if (idx >= 0)
            {
                current.RemoveAt(idx);
                selectedMods.Value = current;
                return false;
            }

            current.RemoveAll(m => match.IncompatibleMods.Any(t => t.IsInstanceOfType(m)));
            current.Add(match);
            selectedMods.Value = current;
            return true;
        }

        // ---- network -------------------------------------------------------------
        public bool NetworkEnabled => networkEnabled;

        public void SetNetwork(bool enabled)
        {
            networkEnabled = enabled;

            if (api == null)
                return;

            if (!enabled)
                api.Logout();
            else
            {
                string user = localConfig?.Get<string>(OsuSetting.Username) ?? string.Empty;
                if (!string.IsNullOrEmpty(user))
                    api.Login(user, string.Empty);
            }
        }

        public string Endpoint => api?.Endpoints?.WebsiteUrl ?? "https://dev.ppy.sh";

        // ---- rendering / perf ----------------------------------------------------
        public bool FpsDisplayVisible
        {
            get => localConfig?.Get<bool>(OsuSetting.ShowFpsDisplay) ?? false;
            set => localConfig?.SetValue(OsuSetting.ShowFpsDisplay, value);
        }

        public string FrameSyncMode
        {
            get => frameworkConfig?.Get<FrameSync>(FrameworkSetting.FrameSync).GetDescription() ?? "unknown";
            set
            {
                if (frameworkConfig == null)
                    return;

                foreach (FrameSync fs in Enum.GetValues<FrameSync>())
                {
                    if (fs.GetDescription().Replace(" ", "").Equals(value.Replace(" ", ""), StringComparison.OrdinalIgnoreCase)
                        || fs.ToString().Equals(value.Replace(" ", ""), StringComparison.OrdinalIgnoreCase))
                    {
                        frameworkConfig.SetValue(FrameworkSetting.FrameSync, fs);
                        return;
                    }
                }
            }
        }

        public IReadOnlyList<string> FrameSyncOptions => Enum.GetValues<FrameSync>().Select(f => f.GetDescription()).ToList();

        // ---- audio ---------------------------------------------------------------
        public double AudioOffsetMs
        {
            get => localConfig?.Get<double>(OsuSetting.AudioOffset) ?? 0;
            set => localConfig?.SetValue(OsuSetting.AudioOffset, value);
        }

        // ---- diagnostics ---------------------------------------------------------
        public string PerfSnapshot()
        {
            return string.Join('\n', new[]
            {
                $"framesync : {FrameSyncMode}",
                $"fps overlay: {(FpsDisplayVisible ? "on" : "off")}",
                $"mods      : {(ActiveModAcronyms.Count == 0 ? "none" : string.Join(" ", ActiveModAcronyms))}",
                $"network   : {(networkEnabled ? "on" : "off")} ({Endpoint})",
                $"audio off : {AudioOffsetMs:0} ms",
                "tip: use 'benchmark gameplay' for a measured run.",
            });
        }

        public IEnumerable<string> RunGameplayBenchmark()
        {
            yield return "gameplay benchmark must be run via the offline harness:";
            yield return "  ./benchmark.sh    (BenchmarkDotNet)";
            yield return "this avoids reporting unverified in-session numbers.";
        }

        // ---- reloads -------------------------------------------------------------
        public void ReloadSkin() { }
        public void ReloadMap() { }
    }
}
