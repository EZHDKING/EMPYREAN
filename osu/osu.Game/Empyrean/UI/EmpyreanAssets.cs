// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Graphics;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Textures;
using osu.Framework.IO.Stores;
using osu.Framework.Platform;

namespace osu.Game.Empyrean.UI
{
    /// <summary>
    /// Loads the bundled Windows 95/98/2000 .ico-derived PNG icons and retro wallpapers embedded
    /// in the assembly and exposes them as textures/sprites. A missing asset yields a null texture
    /// (callers fall back to a FontAwesome glyph) rather than throwing.
    /// </summary>
    public static class EmpyreanAssets
    {
        private static TextureStore icons;
        private static TextureStore wallpapers;

        /// <summary>
        /// Initialise the texture stores. Call once from a component that can resolve GameHost and
        /// the renderer (e.g. the desktop's BDL). Safe to call multiple times.
        /// </summary>
        public static void Init(GameHost host, IRenderer renderer)
        {
            if (icons != null || host == null || renderer == null)
                return;

            try
            {
                var asm = typeof(EmpyreanAssets).Assembly;
                var resources = new DllResourceStore(asm);

                icons = new TextureStore(renderer, host.CreateTextureLoaderStore(
                    new NamespacedResourceStore<byte[]>(resources, "Empyrean.Resources.Icons")));

                wallpapers = new TextureStore(renderer, host.CreateTextureLoaderStore(
                    new NamespacedResourceStore<byte[]>(resources, "Empyrean.Resources.Wallpapers")));
            }
            catch
            {
                icons = null;
                wallpapers = null;
            }
        }

        public static Texture GetIcon(string name)
        {
            try { return icons?.Get(name + ".png"); }
            catch { return null; }
        }

        public static Texture GetWallpaper(string name)
        {
            try { return wallpapers?.Get(name + ".jpg"); }
            catch { return null; }
        }

        /// <summary>A Sprite for an icon, or null if unavailable (caller falls back to a glyph).</summary>
        public static Sprite IconSprite(string name)
        {
            var tex = GetIcon(name);
            if (tex == null)
                return null;

            return new Sprite { Texture = tex, FillMode = FillMode.Fit };
        }
    }
}
