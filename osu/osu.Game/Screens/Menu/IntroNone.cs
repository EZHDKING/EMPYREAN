// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using System;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Screens;
using osu.Game.Empyrean.UI;

namespace osu.Game.Screens.Menu
{
    /// <summary>
    /// EMPYREAN boot intro.
    ///
    /// Philosophy (PROJECT.md §3.1 / §17): the intro must not be an expensive animated
    /// sequence. It is a near-instant, low-cost boot that shows the flat EMPYREAN logo over a
    /// static screen, then hands straight to the menu. No particle work, no shader-heavy
    /// geometry, no per-frame transforms beyond a single cheap fade, and no audio (the menu
    /// theme is owned by EmpyreanMenuMusic so it plays only on the menu).
    ///
    /// The beatmap source is reused from the bundled circles intro purely so the offline
    /// menu plumbing stays valid; its track is never played (EmpyreanMenuMusic silences it).
    /// </summary>
    public partial class IntroNone : IntroScreen
    {
        protected override string BeatmapHash => "3c8b1fcc9434dbb29e2fb613d3b9eada9d7bb6c125ceb32396c3b53437280c83";

        protected override string BeatmapFile => "circles.osz";

        // How long to let the theme establish before moving to the menu. Kept short; the
        // on-logo dwell time before handing to the menu. Kept short for a near-instant boot.
        private const double intro_dwell = 1800;

        private EmpyreanLogo empyreanLogo;

        public IntroNone([CanBeNull] Func<MainMenu> createNextScreen = null)
            : base(createNextScreen)
        {
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            // Flat vaporwave splash shown over the boot. Cheap static composition (no shaders).
            // Wrapped defensively: this screen is on the forced boot path, so nothing here may
            // throw, or the game would fail to start. Any failure simply falls back to a plain boot.
            try
            {
                AddInternal(empyreanLogo = new EmpyreanLogo
                {
                    Anchor = Anchor.Centre,
                    Origin = Anchor.Centre,
                    Alpha = 0,
                    Depth = float.MinValue, // draw above the standard intro layer
                });
            }
            catch
            {
                empyreanLogo = null;
            }
        }

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            if (!resuming)
            {
                if (empyreanLogo != null)
                {
                    logo.Alpha = 0; // hide the stock osu! logo; EMPYREAN uses its own splash.
                    empyreanLogo.FadeIn(400, Easing.OutQuint);
                }

                // NOTE: menu music is owned by EmpyreanMenuMusic on the MainMenu screen so it
                // plays ONLY on the menu (and stops at song select). We deliberately do NOT
                // start any track here, and we do NOT call StartTrack() (which would start the
                // bundled circles.mp3 beatmap track). The boot is silent until the menu appears.

                PrepareMenuLoad();

                Scheduler.AddDelayed(loadMenuWhenReady, intro_dwell);
            }
        }

        private void loadMenuWhenReady()
        {
            if (NextScreenReady)
                LoadMenu();
            else
                Schedule(loadMenuWhenReady);
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            empyreanLogo?.FadeOut(250);
            this.FadeOut(300);
            base.OnSuspending(e);
        }

    }
}
