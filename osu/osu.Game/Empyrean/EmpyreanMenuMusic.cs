// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Track;
using osu.Framework.Graphics;
using osu.Framework.IO.Stores;
using osu.Game.Configuration;
using osu.Game.Overlays;

namespace osu.Game.Empyrean
{
    /// <summary>
    /// Owns the EMPYREAN menu theme (embedded bgm). It plays ONLY while this component is
    /// active on the main menu, and is paused as soon as the menu is left (e.g. entering song
    /// select), per the project requirement that the theme is a menu-only track.
    ///
    /// It also silences the default beatmap track on the menu so the bundled osu! intro track
    /// (circles.mp3) does not play underneath. Everything is null-guarded and try/caught so a
    /// missing resource never affects gameplay or boot.
    /// </summary>
    public partial class EmpyreanMenuMusic : Component
    {
        private Track theme;
        private Track startup;
        private bool startupPlayed;

        [Resolved(canBeNull: true)]
        private MusicController musicController { get; set; }

        [Resolved(canBeNull: true)]
        private OsuConfigManager config { get; set; }

        [BackgroundDependencyLoader]
        private void load(AudioManager audio)
        {
            try
            {
                var store = new DllResourceStore(typeof(EmpyreanMenuMusic).Assembly);
                var trackStore = audio.GetTrackStore(store);
                theme = trackStore.Get("Empyrean/Resources/Tracks/empyrean-intro.mp3");
                if (theme != null)
                    theme.Looping = true;

                // The Windows-95-style boot jingle, played once on launch.
                startup = trackStore.Get("Empyrean/Resources/Tracks/startup.mp3");
            }
            catch
            {
                theme = null;
                startup = null;
            }
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            PlayStartupJingle();
            PlayMenuTheme();
        }

        /// <summary>Play the boot jingle exactly once, on first load.</summary>
        public void PlayStartupJingle()
        {
            if (startupPlayed || startup == null)
                return;

            startupPlayed = true;

            try
            {
                startup.Volume.Value = 1;
                startup.Restart();
            }
            catch { }
        }

        /// <summary>Start the EMPYREAN theme and silence the default beatmap track.</summary>
        public void PlayMenuTheme()
        {
            // Silence the osu! beatmap/intro track so only the EMPYREAN theme is heard on the menu.
            try
            {
                musicController?.Stop();
                if (musicController != null)
                    musicController.CurrentTrack.Volume.Value = 0;
            }
            catch { }

            bool wantMusic = config?.Get<bool>(OsuSetting.MenuMusic) ?? true;
            if (theme == null || !wantMusic)
                return;

            try
            {
                if (!theme.IsRunning)
                {
                    theme.Volume.Value = 1;
                    theme.Start();
                }
            }
            catch { }
        }

        private bool menuActive = true;

        /// <summary>Called by MainMenu so the theme only manages itself while the menu is active.</summary>
        public void SetMenuActive(bool active) => menuActive = active;

        protected override void Update()
        {
            base.Update();

            // Coordinate the EMPYREAN theme with WinAmp / the beatmap track: while a beatmap track
            // is actually playing (e.g. the user hit Play in WinAmp), pause the looping theme; when
            // that track stops/pauses, bring the theme back. Only do this on the menu.
            if (!menuActive || theme == null || musicController == null)
                return;

            bool wantMusic = config?.Get<bool>(OsuSetting.MenuMusic) ?? true;
            if (!wantMusic)
                return;

            try
            {
                bool beatmapTrackPlaying = musicController.CurrentTrack.IsRunning
                                           && musicController.CurrentTrack.Volume.Value > 0.01;

                if (beatmapTrackPlaying)
                {
                    // WinAmp is producing sound — duck/stop the theme.
                    if (theme.IsRunning)
                        theme.Stop();
                }
                else
                {
                    // Nothing else is playing — resume the theme.
                    if (!theme.IsRunning)
                    {
                        theme.Volume.Value = 1;
                        theme.Start();
                    }
                }
            }
            catch { }
        }

        /// <summary>Stop the EMPYREAN theme and RESTORE the beatmap track volume (called when
        /// leaving the menu). Restoring volume is essential: PlayMenuTheme zeroes the shared
        /// CurrentTrack volume to silence it on the menu, and because osu! reuses that track object
        /// across plays, failing to restore it leaves gameplay music silent on the 2nd+ play.</summary>
        public void StopMenuTheme()
        {
            try
            {
                theme?.Stop();
            }
            catch { }

            try
            {
                if (musicController != null)
                {
                    // Bring the beatmap track back to full volume for gameplay.
                    musicController.CurrentTrack.Volume.Value = 1;
                }
            }
            catch { }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);
            theme?.Dispose();
            startup?.Dispose();
        }
    }
}
