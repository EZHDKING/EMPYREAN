// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Localisation;
using osu.Game.Localisation;

namespace osu.Game.Configuration
{
    public enum IntroSequence
    {
        // EMPYREAN: instant boot straight to main menu. No animation, no intro track,
        // no shader warm-up time spent on decorative geometry. This is the default for
        // a competition-first client (see PROJECT.md §4.3 / §17.1). Kept first so it is
        // the lowest enum value and the obvious "do nothing" path.
        [System.ComponentModel.Description("None (instant)")]
        None,

        Circles,
        Welcome,
        Triangles,

        [LocalisableDescription(typeof(UserInterfaceStrings), nameof(UserInterfaceStrings.IntroRandom))]
        Random
    }
}
