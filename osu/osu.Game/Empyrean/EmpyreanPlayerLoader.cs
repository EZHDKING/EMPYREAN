// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Game.Screens.Play;

namespace osu.Game.Empyrean
{
    /// <summary>
    /// A <see cref="PlayerLoader"/> tuned for EMPYREAN's competitive, no-frills flow: it pushes
    /// into gameplay as soon as the player is actually loaded, with no artificial dwell. The base
    /// loader waits ~1.8s on a metadata splash; here we drop that to the minimum so a double-click
    /// in the Win95 desktop launches the map essentially immediately. The base class still gates
    /// the push on the player being fully loaded, so this stays safe.
    /// </summary>
    public partial class EmpyreanPlayerLoader : PlayerLoader
    {
        // Minimal delay (base default is 1800 + disclaimers*500). A small non-zero value keeps the
        // ready-check loop happy without a visible loading sequence.
        protected override double PlayerPushDelay => 0;

        public EmpyreanPlayerLoader(Func<Player> createPlayer)
            : base(createPlayer)
        {
        }
    }
}
