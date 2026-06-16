// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

namespace osu.Game.Empyrean
{
    /// <summary>
    /// Central identity and branding for the EMPYREAN fork.
    ///
    /// EMPYREAN is a competition-first, performance-extreme fork of osu!lazer.
    /// See PROJECT.md (the AI Agent Master Guide) for the full doctrine. The single
    /// guiding question for every change is: does it improve raw gameplay performance,
    /// input latency, frametime stability, or competitive reliability?
    /// </summary>
    public static class EmpyreanInfo
    {
        public const string PRODUCT_NAME = "EMPYREAN";

        public const string CREATOR = "EZHD KING";

        public const string TAGLINE = "osu!lazer, but better";

        /// <summary>
        /// Short DOS-style banner printed by the terminal and (optionally) at boot.
        /// Kept as a single interpolated string to avoid array allocation on access.
        /// </summary>
        public static string Banner =>
            $"{PRODUCT_NAME} — {TAGLINE}\n" +
            $"Creator: {CREATOR}\n" +
            "Type 'help' for commands. Every frame matters.";
    }
}
