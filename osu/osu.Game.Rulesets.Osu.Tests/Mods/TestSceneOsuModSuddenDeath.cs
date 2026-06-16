// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using osu.Framework.Localisation;
using osu.Game.Rulesets.Mods;
using osu.Game.Rulesets.Objects;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osu.Game.Rulesets.Osu.Replays;
using osu.Game.Rulesets.Replays;
using osu.Game.Rulesets.UI;
using osu.Game.Screens.Play;

namespace osu.Game.Rulesets.Osu.Mods
{
    public partial class OsuModSuddenDeath : ModSuddenDeath, IApplicableToDrawableRuleset<OsuHitObject>, IApplicableToPlayer, IUpdatableByPlayfield
    {
        public override LocalisableString Description => @"Sudden Death but it plays like Relax!";
        
        // Force it to be ranked
        public override bool Ranked => true;

        public override Type[] IncompatibleMods => base.IncompatibleMods.Concat(new[] { typeof(OsuModAutopilot), typeof(OsuModMagnetised), typeof(OsuModAlternate), typeof(OsuModSingleTap) }).ToArray();

        public const float RELAX_LENIENCY = 12;

        private bool isDownState;
        private bool wasLeft;

        private OsuInputManager osuInputManager = null!;
        private ReplayState<OsuAction> state = null!;
        private double lastStateChangeTime;
        private DrawableOsuRuleset ruleset = null!;
        private IPressHandler pressHandler = null!;
        private bool hasReplay;
        private bool legacyReplay;

        public void ApplyToDrawableRuleset(DrawableRuleset<OsuHitObject> drawableRuleset)
        {
            ruleset = (DrawableOsuRuleset)drawableRuleset;
            osuInputManager = ruleset.KeyBindingInputManager;
        }

        public void ApplyToPlayer(Player player)
        {
            // Run the normal Sudden Death initialization first
            base.ApplyToPlayer(player);

            if (osuInputManager.ReplayInputHandler != null)
            {
                hasReplay = true;
                Debug.Assert(ruleset.ReplayScore != null);
                legacyReplay = ruleset.ReplayScore.ScoreInfo.IsLegacyScore;
                pressHandler = legacyReplay ? new LegacyReplayPressHandler(this) : new PressHandler(this);
                return;
            }

            pressHandler = new PressHandler(this);
            osuInputManager.AllowGameplayInputs = false;
        }

        public void Update(Playfield playfield)
        {
            if (hasReplay && !legacyReplay)
                return;

            bool requiresHold = false;
            bool requiresHit = false;
            double time = playfield.Clock.CurrentTime;

            foreach (var h in playfield.HitObjectContainer.AliveObjects.OfType<DrawableOsuHitObject>())
            {
                if (time < h.HitObject.StartTime - RELAX_LENIENCY)
                    break;

                if (h.IsHit || (h.HitObject is IHasDuration hasEnd && time > hasEnd.EndTime))
                    continue;

                switch (h)
                {
                    case DrawableHitCircle circle:
                        handleHitCircle(circle);
                        break;

                    case DrawableSlider slider:
                        if (!slider.HeadCircle.IsHit)
                            handleHitCircle(slider.HeadCircle);

                        requiresHold |= slider.SliderInputManager.IsMouseInFollowArea(slider.Tracking.Value);
                        break;

                    case DrawableSpinner spinner:
                        requiresHold |= spinner.HitObject.SpinsRequired > 0;
                        break;
                }
            }

            if (requiresHit)
            {
                changeState(false);
                changeState(true);
            }

            if (requiresHold)
                changeState(true);
            else if (isDownState && time - lastStateChangeTime > AutoGenerator.KEY_UP_DELAY)
                changeState(false);

            void handleHitCircle(DrawableHitCircle circle)
            {
                if (!circle.HitArea.IsHovered)
                    return;

                Debug.Assert(circle.HitObject.HitWindows != null);
                requiresHit |= circle.HitObject.HitWindows.CanBeHit(time - circle.HitObject.StartTime);
            }

            void changeState(bool down)
            {
                if (isDownState == down)
                    return;

                isDownState = down;
                lastStateChangeTime = time;

                state = new ReplayState<OsuAction>
                {
                    PressedActions = new List<OsuAction>()
                };

                if (down)
                {
                    pressHandler.HandlePress(wasLeft);
                    wasLeft = !wasLeft;
                }
                else
                {
                    pressHandler.HandleRelease(wasLeft);
                }
            }
        }

        private interface IPressHandler
        {
            void HandlePress(bool wasLeft);
            void HandleRelease(bool wasLeft);
        }

        private class PressHandler : IPressHandler
        {
            private readonly OsuModSuddenDeath mod;
            public PressHandler(OsuModSuddenDeath mod) => this.mod = mod;

            public void HandlePress(bool wasLeft)
            {
                mod.state.PressedActions.Add(wasLeft ? OsuAction.LeftButton : OsuAction.RightButton);
                mod.state.Apply(mod.osuInputManager.CurrentState, mod.osuInputManager);
            }

            public void HandleRelease(bool wasLeft)
            {
                mod.state.Apply(mod.osuInputManager.CurrentState, mod.osuInputManager);
            }
        }

        private class LegacyReplayPressHandler : IPressHandler
        {
            private readonly OsuModSuddenDeath mod;
            public LegacyReplayPressHandler(OsuModSuddenDeath mod) => this.mod = mod;

            public void HandlePress(bool wasLeft)
            {
                mod.osuInputManager.KeyBindingContainer.TriggerPressed(wasLeft ? OsuAction.LeftButton : OsuAction.RightButton);
            }

            public void HandleRelease(bool wasLeft)
            {
                mod.osuInputManager.KeyBindingContainer.TriggerReleased(wasLeft ? OsuAction.RightButton : OsuAction.LeftButton);
            }
        }
    }
}