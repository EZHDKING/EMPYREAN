// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Localisation;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.UserInterfaceV2;

namespace osu.Game.Overlays.Settings.Sections
{
    /// <summary>
    /// EMPYREAN settings section. Also serves as the visible confirmation that the EMPYREAN
    /// fork is actually loaded — if you see this section in Settings, the fork code is running.
    /// </summary>
    public partial class EmpyreanSection : SettingsSection
    {
        public override LocalisableString Header => "EMPYREAN";

        public override Drawable CreateIcon() => new SpriteIcon
        {
            Icon = OsuIcon.Logo,
        };

        public EmpyreanSection()
        {
            Children = new Drawable[]
            {
                new EmpyreanSettings(),
            };
        }

        private partial class EmpyreanSettings : SettingsSubsection
        {
            protected override LocalisableString Header => "Competitive";

            [BackgroundDependencyLoader]
            private void load(OsuConfigManager config)
            {
                Children = new Drawable[]
                {
                    new SettingsItemV2(new FormCheckBox
                    {
                        Caption = "Flat gameplay rendering",
                        HintText = "Minimal hit-circle rendering: no glow, no animated triangles, "
                                   + "no additive flash/explode overdraw. The biggest gameplay-path GPU win.",
                        Current = config.GetBindable<bool>(OsuSetting.EmpyreanFlatGameplay),
                    }),
                    new SettingsItemV2(new FormCheckBox
                    {
                        Caption = "Show storyboards",
                        Current = config.GetBindable<bool>(OsuSetting.ShowStoryboard),
                    }),
                    new SettingsItemV2(new FormCheckBox
                    {
                        Caption = "Hit lighting",
                        Current = config.GetBindable<bool>(OsuSetting.HitLighting),
                    }),
                    new SettingsItemV2(new FormCheckBox
                    {
                        Caption = "Star fountains",
                        Current = config.GetBindable<bool>(OsuSetting.StarFountains),
                    }),
                    new SettingsItemV2(new FormCheckBox
                    {
                        Caption = "Menu parallax",
                        Current = config.GetBindable<bool>(OsuSetting.MenuParallax),
                    }),
                    new SettingsItemV2(new FormCheckBox
                    {
                        Caption = "Cursor rotation",
                        Current = config.GetBindable<bool>(OsuSetting.CursorRotation),
                    }),
                };
            }
        }
    }
}
