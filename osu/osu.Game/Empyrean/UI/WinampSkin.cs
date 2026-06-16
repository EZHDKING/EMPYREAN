// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.IO;
using System.IO.Compression;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Textures;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// Loads a classic WinAmp 2.x skin (.wsz = a ZIP of BMPs) from the embedded skin resources and
    /// exposes the standard sprite-sheet bitmaps (Main, Cbuttons, Numbers, Titlebar, Posbar,
    /// Volume, Text…) as textures. The WinAmp window composites its UI from regions of these.
    ///
    /// .wsz files are matched case-insensitively (skin authors vary the casing of e.g. MAIN.BMP).
    /// A missing bitmap simply yields null and the window falls back to a flat panel.
    /// </summary>
    public class WinampSkin
    {
        // Logical resource names of the bundled skins (no extension), and a friendly display name.
        public static readonly (string id, string name)[] BundledSkins =
        {
            ("Classic", "Classic"),
            ("Winamp5Classifiedv55", "Winamp 5 Classified"),
            ("Winamp3Classifiedv55", "Winamp 3 Classified"),
            ("BentoClassified", "Bento Classified"),
            ("FalloutPipBoy200012", "Fallout Pip-Boy"),
            ("DeusExAmpbyAJ", "Deus Ex"),
            ("ZeldaAmp3", "Zelda Amp"),
            ("Microchip2", "Microchip"),
            ("DoritosNachoAmp", "Doritos NachoAmp"),
            ("Garfield", "Garfield"),
            ("Morbamp", "Morbamp"),
            ("Mrbeanamp2", "Mr Bean Amp"),
        };

        private readonly Dictionary<string, Texture> bitmaps = new Dictionary<string, Texture>(StringComparer.OrdinalIgnoreCase);

        public Texture Main => get("main");
        public Texture CButtons => get("cbuttons");
        public Texture Numbers => get("numbers");          // some skins use nums_ex
        public Texture Titlebar => get("titlebar");
        public Texture Posbar => get("posbar");
        public Texture Volume => get("volume");
        public Texture Text => get("text");
        public Texture Monoster => get("monoster");
        public Texture Playpaus => get("playpaus");

        private Texture get(string name)
        {
            if (bitmaps.TryGetValue(name + ".bmp", out var t))
                return t;
            // fallbacks for common alternates
            if (name == "numbers" && bitmaps.TryGetValue("nums_ex.bmp", out var n))
                return n;
            return null;
        }

        /// <summary>
        /// Load a bundled skin by id. Returns a populated <see cref="WinampSkin"/> or null on failure.
        /// </summary>
        public static WinampSkin Load(string id, IRenderer renderer)
        {
            if (renderer == null)
                return null;

            try
            {
                var asm = typeof(WinampSkin).Assembly;
                string resourceName = $"osu.Game.Empyrean.Resources.Skins.{id}.wsz";

                using var zipStream = asm.GetManifestResourceStream(resourceName);
                if (zipStream == null)
                    return null;

                // Copy to memory so we can seek within the ZIP.
                using var ms = new MemoryStream();
                zipStream.CopyTo(ms);
                ms.Position = 0;

                var skin = new WinampSkin();

                using (var archive = new ZipArchive(ms, ZipArchiveMode.Read))
                {
                    foreach (var entry in archive.Entries)
                    {
                        if (!entry.FullName.EndsWith(".bmp", StringComparison.OrdinalIgnoreCase))
                            continue;

                        // Use just the file name (skins sometimes nest in a folder).
                        string key = Path.GetFileName(entry.FullName).ToLowerInvariant();

                        try
                        {
                            using var es = entry.Open();
                            using var bmpMs = new MemoryStream();
                            es.CopyTo(bmpMs);
                            bmpMs.Position = 0;

                            var tex = Texture.FromStream(renderer, bmpMs);
                            if (tex != null)
                                skin.bitmaps[key] = tex;
                        }
                        catch
                        {
                            // skip a single bad bitmap
                        }
                    }
                }

                // Return the skin even if only some bitmaps loaded; the window falls back per-sprite.
                return skin;
            }
            catch
            {
                return null;
            }
        }
    }
}
