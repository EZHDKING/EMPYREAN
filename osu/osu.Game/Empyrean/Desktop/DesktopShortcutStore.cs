// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using osu.Framework.Logging;
using osu.Framework.Platform;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A persisted desktop item — either a single beatmap-difficulty shortcut, or a folder
    /// ("Map Collection") that holds several beatmap shortcuts. Stores its own desktop position
    /// so icons stay where the user dragged them.
    /// </summary>
    [Serializable]
    public class BeatmapShortcut
    {
        public string Label { get; set; } = string.Empty;
        public string BeatmapId { get; set; } = string.Empty; // BeatmapInfo.ID (Guid) as string

        // Freeform desktop position (px from top-left of the icon area). -1 = unplaced (auto-arrange).
        public float X { get; set; } = -1;
        public float Y { get; set; } = -1;

        // Folder support: when IsFolder is true, BeatmapId is ignored and Items holds the maps.
        public bool IsFolder { get; set; }
        public List<BeatmapShortcut> Items { get; set; } = new List<BeatmapShortcut>();
    }

    /// <summary>
    /// The full persisted desktop state: items + user preferences (icon size).
    /// </summary>
    [Serializable]
    public class DesktopState
    {
        public List<BeatmapShortcut> Items { get; set; } = new List<BeatmapShortcut>();
        public float IconSize { get; set; } = 32;
    }

    /// <summary>
    /// Persists the EMPYREAN desktop (icons, folders, positions, icon size) to a JSON file in osu!
    /// storage so the desktop is restored between sessions, like real Windows 95 desktop icons.
    /// </summary>
    public class DesktopShortcutStore
    {
        private const string filename = "empyrean-desktop.json";

        private readonly Storage storage;

        public DesktopShortcutStore(Storage storage)
        {
            this.storage = storage;
        }

        public DesktopState Load()
        {
            try
            {
                if (storage == null || !storage.Exists(filename))
                    return new DesktopState();

                using var stream = storage.GetStream(filename, FileAccess.Read);
                using var reader = new StreamReader(stream);
                string json = reader.ReadToEnd();
                return JsonConvert.DeserializeObject<DesktopState>(json) ?? new DesktopState();
            }
            catch (Exception ex)
            {
                Logger.Log($"EMPYREAN: failed to load desktop state: {ex.Message}", LoggingTarget.Runtime);
                return new DesktopState();
            }
        }

        public void Save(DesktopState state)
        {
            try
            {
                if (storage == null)
                    return;

                string json = JsonConvert.SerializeObject(state, Formatting.Indented);
                using var stream = storage.CreateFileSafely(filename);
                using var writer = new StreamWriter(stream);
                writer.Write(json);
            }
            catch (Exception ex)
            {
                Logger.Log($"EMPYREAN: failed to save desktop state: {ex.Message}", LoggingTarget.Runtime);
            }
        }
    }
}
