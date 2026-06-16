// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

#nullable disable

using osu.Game.Utils;
using osu.Game.Rulesets.Mods;
using System.Collections.Generic;
using System;
using System.Diagnostics;
using System.Linq;
using JetBrains.Annotations;
using osu.Framework.Allocation;
using osu.Framework.Audio;
using osu.Framework.Audio.Sample;
using osu.Framework.Bindables;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Bindings;
using osu.Framework.Input.Events;
using osu.Framework.Logging;
using osu.Framework.Platform;
using osu.Framework.Screens;
using osu.Framework.Threading;
using osu.Game.Audio;
using osu.Game;
using osu.Game.Beatmaps;
using osu.Game.Configuration;
using osu.Game.Graphics;
using osu.Game.Graphics.Containers;
using osu.Game.Input.Bindings;
using osu.Game.IO;
using osu.Game.Localisation;
using osu.Game.Online.API;
using osu.Game.Online.Matchmaking;
using osu.Game.Overlays;
using osu.Game.Overlays.Dialog;
using osu.Game.Overlays.SkinEditor;
using osu.Game.Overlays.Volume;
using osu.Game.Rulesets;
using osu.Game.Screens.Backgrounds;
using osu.Game.Screens.Edit;
using osu.Game.Screens.OnlinePlay.DailyChallenge;
using osu.Game.Screens.OnlinePlay.Multiplayer;
using osu.Game.Screens.OnlinePlay.Playlists;
using osu.Game.Screens.Select;
using osu.Game.Seasonal;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Screens.Menu
{
    public partial class MainMenu : OsuScreen, IHandlePresentBeatmap, IKeyBindingHandler<GlobalAction>, ISamplePlaybackDisabler
    {
        public const float FADE_IN_DURATION = 300;

        public const float FADE_OUT_DURATION = 400;

        public override bool HideOverlaysOnEnter => Buttons == null || Buttons.State == ButtonSystemState.Initial;

        public override bool AllowUserExit => false;

        public override bool AllowExternalScreenChange => true;

        public override bool? AllowGlobalTrackControl => true;

        private MenuSideFlashes sideFlashes;

        protected ButtonSystem Buttons;

        [Resolved]
        private GameHost host { get; set; }

        [Resolved]
        private INotificationOverlay notifications { get; set; }

        [Resolved]
        private MusicController musicController { get; set; }

        [Resolved]
        private BeatmapManager beatmapManager { get; set; }

        // EMPYREAN: menu-only theme music manager.
        private osu.Game.Empyrean.EmpyreanMenuMusic empyreanMenuMusic;

        // EMPYREAN: the Windows 95 desktop shell overlaying the menu.
        private osu.Game.Empyrean.Desktop.Win95Desktop empyreanDesktop;

        // EMPYREAN: the Win95 control panel that replaces the modern settings overlay.
        private osu.Game.Empyrean.Overlays.EmpyreanControlPanel empyreanControlPanel;

        [Resolved]
        private IAPIProvider api { get; set; }

        [Resolved(canBeNull: true)]
        private osu.Game.Online.Metadata.MetadataClient metadataClient { get; set; }

        [Resolved]
        private Storage storage { get; set; }

        [Resolved(canBeNull: true)]
        private LoginOverlay login { get; set; }

        [Resolved(canBeNull: true)]
        private IDialogOverlay dialogOverlay { get; set; }

        // used to stop kiai fountain samples when navigating to other screens
        IBindable<bool> ISamplePlaybackDisabler.SamplePlaybackDisabled => samplePlaybackDisabled;
        private readonly Bindable<bool> samplePlaybackDisabled = new Bindable<bool>();

        protected override BackgroundScreen CreateBackground() => new BackgroundScreenDefault();

        protected override bool PlayExitSound => false;

        private Bindable<double> holdDelay;
        private Bindable<bool> loginDisplayed;
        private Bindable<bool> showMobileDisclaimer;

        private HoldToExitGameOverlay holdToExitGameOverlay;

        private bool exitConfirmedViaDialog;
        private bool exitConfirmedViaHoldOrClick;

        private ParallaxContainer buttonsContainer;
        private SongTicker songTicker;
        private Container logoTarget;
        private OnlineMenuBanner onlineMenuBanner;
        private MenuTipDisplay menuTipDisplay;
        private FillFlowContainer bottomElementsFlow;
        private SupporterDisplay supporterDisplay;

        private Sample reappearSampleSwoosh;

        [Resolved(canBeNull: true)]
        private SkinEditorOverlay skinEditor { get; set; }

        [CanBeNull]
        private IDisposable logoProxy;

        [BackgroundDependencyLoader(true)]
        private void load(BeatmapListingOverlay beatmapListing, SettingsOverlay settings, OsuConfigManager config, SessionStatics statics, AudioManager audio)
        {
            // EMPYREAN: menu-only theme music. Owns bgm and silences the default beatmap track
            // so only the EMPYREAN theme plays on the menu (and stops at song select).
            AddInternal(empyreanMenuMusic = new osu.Game.Empyrean.EmpyreanMenuMusic());

            // EMPYREAN: the Windows 95 desktop shell. Drawn over the stock menu and wired to the
            // real game actions. "Play osu!" runs the normal solo flow; the beatmap browser
            // presents maps; Settings opens the (now 95-themed) settings overlay.
            // EMPYREAN: the Win95 control panel (replaces the modern settings overlay on the desktop).
            // MinValue depth = drawn in front of everything, including the desktop.
            AddInternal(empyreanControlPanel = new osu.Game.Empyrean.Overlays.EmpyreanControlPanel
            {
                Depth = float.MinValue,
            });

            AddInternal(empyreanDesktop = new osu.Game.Empyrean.Desktop.Win95Desktop
            {
                Depth = -10000f, // in front of the stock menu, behind the control panel
                OnLaunchPlay = loadSongSelect,
                OnOpenSettings = () => empyreanControlPanel?.ToggleVisibility(),
                OnExit = () => this.Exit(),
                OnPlayBeatmap = PlayBeatmapDirect,
                OnOpenBeatmapDownloader = () => beatmapListing?.ToggleVisibility(),
                OnOpenEditor = () => this.Push(new EditorLoader()),
                OnOpenSkinEditor = () => skinEditor?.Show(),
                OnOpenProfile = () =>
                {
                    var localUser = api?.LocalUser?.Value;
                    if (localUser != null && localUser.Id > 1)
                        (Game as OsuGame)?.ShowUser(localUser);
                },
                OnOpenMultiplayer = () => this.Push(new Multiplayer()),
                OnOpenPlaylists = () => this.Push(new Playlists()),
                OnOpenRankedPlay = loadRankedPlay,
                OnOpenDailyChallenge = openDailyChallenge,
            });

            holdDelay = config.GetBindable<double>(OsuSetting.UIHoldActivationDelay);
            loginDisplayed = statics.GetBindable<bool>(Static.LoginOverlayDisplayed);
            showMobileDisclaimer = config.GetBindable<bool>(OsuSetting.ShowMobileDisclaimer);

            if (host.CanExit)
            {
                AddInternal(holdToExitGameOverlay = new HoldToExitGameOverlay
                {
                    Action = () =>
                    {
                        exitConfirmedViaHoldOrClick = holdDelay.Value > 0;
                        this.Exit();
                    }
                });
            }

            AddRangeInternal(new[]
            {
                SeasonalUIConfig.ENABLED ? new MainMenuSeasonalLighting() : Empty(),
                new GlobalScrollAdjustsVolume(),
                buttonsContainer = new ParallaxContainer
                {
                    ParallaxAmount = 0.01f,
                    Children = new Drawable[]
                    {
                        Buttons = new ButtonSystem
                        {
                            OnEditBeatmap = () =>
                            {
                                Beatmap.SetDefault();
                                this.Push(new EditorLoader());
                            },
                            OnEditSkin = () =>
                            {
                                skinEditor?.Show();
                            },
                            OnSolo = loadSongSelect,
                            OnMultiplayer = () => this.Push(new Multiplayer()),
                            OnQuickPlay = loadQuickPlay,
                            OnRankedPlay = loadRankedPlay,
                            OnPlaylists = () => this.Push(new Playlists()),
                            OnDailyChallenge = room =>
                            {
                                if (statics.Get<bool>(Static.DailyChallengeIntroPlayed))
                                    this.Push(new DailyChallenge(room));
                                else
                                    this.Push(new DailyChallengeIntro(room));
                            },
                            OnExit = e =>
                            {
                                exitConfirmedViaHoldOrClick = e is MouseEvent;
                                this.Exit();
                            }
                        }
                    }
                },
                logoTarget = new Container { RelativeSizeAxes = Axes.Both, },
                sideFlashes = SeasonalUIConfig.ENABLED ? new SeasonalMenuSideFlashes() : new MenuSideFlashes(),
                songTicker = new SongTicker
                {
                    Anchor = Anchor.TopRight,
                    Origin = Anchor.TopRight,
                    Margin = new MarginPadding { Right = 15, Top = 5 }
                },
                // For now, this is too much alongside the seasonal lighting.
                SeasonalUIConfig.ENABLED ? Empty() : new KiaiMenuFountains(),
                bottomElementsFlow = new FillFlowContainer
                {
                    AutoSizeAxes = Axes.Both,
                    Direction = FillDirection.Vertical,
                    Anchor = Anchor.BottomCentre,
                    Origin = Anchor.BottomCentre,
                    Spacing = new Vector2(5),
                    Children = new Drawable[]
                    {
                        menuTipDisplay = new MenuTipDisplay
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                        },
                        onlineMenuBanner = new OnlineMenuBanner
                        {
                            Anchor = Anchor.TopCentre,
                            Origin = Anchor.TopCentre,
                        }
                    }
                },
                supporterDisplay = new SupporterDisplay
                {
                    Margin = new MarginPadding(5),
                    Anchor = Anchor.TopLeft,
                    Origin = Anchor.TopLeft,
                },
                holdToExitGameOverlay?.CreateProxy() ?? Empty()
            });

            float baseDim = SeasonalUIConfig.ENABLED ? 0.84f : 1;

            Buttons.StateChanged += state =>
            {
                switch (state)
                {
                    case ButtonSystemState.Initial:
                    case ButtonSystemState.Exit:
                        ApplyToBackground(b => b.FadeColour(OsuColour.Gray(baseDim), 500, Easing.OutSine));
                        onlineMenuBanner.State.Value = Visibility.Hidden;
                        break;

                    default:
                        ApplyToBackground(b => b.FadeColour(OsuColour.Gray(baseDim * 0.8f), 500, Easing.OutSine));
                        onlineMenuBanner.State.Value = Visibility.Visible;
                        break;
                }
            };

            Buttons.OnSettings = () => settings?.ToggleVisibility();
            Buttons.OnBeatmapListing = () => beatmapListing?.ToggleVisibility();

            reappearSampleSwoosh = audio.Samples.Get(@"Menu/reappear-swoosh");
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();
            GetContainingInputManager();

            // EMPYREAN: the Win95 desktop fully replaces the osu! menu, so hide the stock menu
            // visuals AND stop them receiving input — otherwise the invisible osu! buttons behind
            // the desktop still catch clicks. AlwaysPresent=false + Alpha 0 + no input handling.
            if (buttonsContainer != null)
            {
                buttonsContainer.Alpha = 0;
                buttonsContainer.AlwaysPresent = false;
            }

            if (sideFlashes != null)
                sideFlashes.Alpha = 0;
        }

        // Block the stock menu's input entirely while the EMPYREAN desktop is the active shell.
        public override bool ReceivePositionalInputAt(osuTK.Vector2 screenSpacePos) => true;

        public void ReturnToOsuLogo() => Buttons.State = ButtonSystemState.Initial;

        public override void OnEntering(ScreenTransitionEvent e)
        {
            base.OnEntering(e);

            // EMPYREAN: stock menu buttons stay hidden — the desktop is the shell.
            empyreanMenuMusic?.SetMenuActive(true);
            empyreanMenuMusic?.PlayMenuTheme();

            if (storage is OsuStorage osuStorage && osuStorage.Error != OsuStorageError.None)
                dialogOverlay?.Push(new StorageErrorDialog(osuStorage, osuStorage.Error));
        }

        [CanBeNull]
        private ScheduledDelegate mobileDisclaimerSchedule;

        protected override void LogoArriving(OsuLogo logo, bool resuming)
        {
            base.LogoArriving(logo, resuming);

            Buttons.SetOsuLogo(logo);

            // EMPYREAN: hide the osu! logo on the menu — the Win95 desktop is the shell.
            logo.FadeOut(100);

            logoProxy = logo.ProxyToContainer(logoTarget);

            if (resuming)
            {
                Buttons.State = ButtonSystemState.TopLevel;

                this.FadeIn(FADE_IN_DURATION, Easing.OutQuint);
                buttonsContainer.MoveTo(new Vector2(0, 0), FADE_IN_DURATION, Easing.OutQuint);

                sideFlashes.Delay(FADE_IN_DURATION).FadeIn(64, Easing.InQuint);
            }
            else
            {
                // copy out old action to avoid accidentally capturing logo.Action in closure, causing a self-reference loop.
                var previousAction = logo.Action;

                // we want to hook into logo.Action to display certain overlays, but also preserve the return value of the old action.
                // therefore pass the old action to displayLogin, so that it can return that value.
                // this ensures that the OsuLogo sample does not play when it is not desired.
                logo.Action = () => onLogoClick(previousAction);
            }
        }

        private bool onLogoClick(Func<bool> originalAction)
        {
            if (showMobileDisclaimer.Value)
            {
                mobileDisclaimerSchedule?.Cancel();
                mobileDisclaimerSchedule = Scheduler.AddDelayed(() =>
                {
                    dialogOverlay.Push(new MobileDisclaimerDialog(() =>
                    {
                        showMobileDisclaimer.Value = false;
                        displayLoginIfApplicable();
                    }));
                }, 500);
            }
            else
                displayLoginIfApplicable();

            return originalAction.Invoke();
        }

        private void displayLoginIfApplicable()
        {
            if (loginDisplayed.Value) return;

            if (!api.IsLoggedIn || api.State.Value == APIState.RequiresSecondFactorAuth)
            {
                Scheduler.AddDelayed(() => login?.Show(), 500);
                loginDisplayed.Value = true;
            }
        }

        protected override void LogoSuspending(OsuLogo logo)
        {
            var seq = logo.FadeOut(300, Easing.InSine)
                          .ScaleTo(0.2f, 300, Easing.InSine);

            logoProxy?.Dispose();
            logoProxy = null;

            seq.OnComplete(_ => Buttons.SetOsuLogo(null));
            seq.OnAbort(_ => Buttons.SetOsuLogo(null));
        }

        protected override void LogoExiting(OsuLogo logo)
        {
            base.LogoExiting(logo);

            logoProxy?.Dispose();
            logoProxy = null;
        }

        public override void OnSuspending(ScreenTransitionEvent e)
        {
            base.OnSuspending(e);

            Buttons.State = ButtonSystemState.EnteringMode;

            this.FadeOut(FADE_OUT_DURATION, Easing.InSine);
            buttonsContainer.MoveTo(new Vector2(-800, 0), FADE_OUT_DURATION, Easing.InSine);

            sideFlashes.FadeOut(64, Easing.OutQuint);

            bottomElementsFlow
                .ScaleTo(0.9f, 1000, Easing.OutQuint)
                .FadeOut(500, Easing.OutQuint);

            supporterDisplay
                .FadeOut(500, Easing.OutQuint);

            samplePlaybackDisabled.Value = true;

            // EMPYREAN: theme is a menu-only track — stop it as soon as we leave the menu.
            empyreanMenuMusic?.SetMenuActive(false);
            empyreanMenuMusic?.StopMenuTheme();
        }

        public override void OnResuming(ScreenTransitionEvent e)
        {
            base.OnResuming(e);

            // Ensures any playing `ButtonSystem` samples are stopped when returning to MainMenu (as to not overlap with the 'back' sample)
            Buttons.StopSamplePlayback();
            reappearSampleSwoosh?.Play();

            ApplyToBackground(b => (b as BackgroundScreenDefault)?.Next());

            // EMPYREAN: restart the menu theme (and re-silence the beatmap track) when we
            // return to the menu from song select / elsewhere.
            empyreanMenuMusic?.SetMenuActive(true);
            empyreanMenuMusic?.PlayMenuTheme();

            // Cycle tip on resuming
            menuTipDisplay.ShowNextTip();

            bottomElementsFlow
                .ScaleTo(1, 1000, Easing.OutQuint)
                .FadeIn(1000, Easing.OutQuint);

            samplePlaybackDisabled.Value = false;
        }

        public override bool OnExiting(ScreenExitEvent e)
        {
            bool requiresConfirmation =
                // we need to have a dialog overlay to confirm in the first place.
                dialogOverlay != null
                // if the dialog has already displayed and been accepted by the user, we are good.
                && !exitConfirmedViaDialog
                // Only require confirmation if there is either an ongoing operation or the user exited via a non-hold escape press.
                && (notifications.HasOngoingOperations || !exitConfirmedViaHoldOrClick);

            if (requiresConfirmation)
            {
                if (dialogOverlay.CurrentDialog is ConfirmExitDialog exitDialog)
                {
                    if (exitDialog.Buttons.OfType<PopupDialogOkButton>().FirstOrDefault() != null)
                        exitDialog.PerformOkAction();
                    else
                        exitDialog.Flash();
                }
                else
                {
                    dialogOverlay.Push(new ConfirmExitDialog(() =>
                    {
                        exitConfirmedViaDialog = true;
                        this.Exit();
                    }, () =>
                    {
                        holdToExitGameOverlay.Abort();
                        Game.CancelRestartOnExit();
                    }));
                }

                return true;
            }

            Buttons.State = ButtonSystemState.Exit;
            OverlayActivationMode.Value = OverlayActivation.Disabled;

            songTicker.Hide();

            this.FadeOut(3000);

            bottomElementsFlow
                .FadeOut(500, Easing.OutQuint);

            supporterDisplay
                .FadeOut(500, Easing.OutQuint);

            return base.OnExiting(e);
        }

        public void PresentBeatmap(WorkingBeatmap beatmap, RulesetInfo ruleset)
        {
            Logger.Log($"{nameof(MainMenu)} completing {nameof(PresentBeatmap)} with beatmap {beatmap} ruleset {ruleset}");

            Beatmap.Value = beatmap;
            Ruleset.Value = ruleset;

            Schedule(loadSongSelect);
        }

        public bool OnPressed(KeyBindingPressEvent<GlobalAction> e)
        {
            if (e.Repeat)
                return false;

            switch (e.Action)
            {
                case GlobalAction.Back:
                    // In the case of a host being able to exit, the back action is handled by ExitConfirmOverlay.
                    Debug.Assert(!host.CanExit);

                    return host.SuspendToBackground();
            }

            return false;
        }

        public void OnReleased(KeyBindingReleaseEvent<GlobalAction> e)
        {
        }

        private void loadSongSelect() => this.Push(new SoloSongSelect());

        /// <summary>
        /// EMPYREAN: launch a specific difficulty straight into gameplay, bypassing song select
        /// entirely. This is how the Win95 desktop "double-click a difficulty" flow plays a map
        /// instantly. Sets the working beatmap + ruleset, then pushes the player loader.
        /// </summary>
        public void PlayBeatmapDirect(BeatmapInfo beatmapInfo)
        {
            if (beatmapInfo == null || beatmapManager == null)
                return;

            // Use the game's PerformFromScreen so navigation is safe regardless of what overlays
            // or desktop windows are open: it returns to the menu, then runs our action. We push
            // PlayerLoader/SoloPlayer directly so the map starts immediately (no song select).
            var game = Game as OsuGame;

            void launch()
            {
                try
                {
                    var working = beatmapManager.GetWorkingBeatmap(beatmapInfo);
                    if (working == null)
                        return;

                    // Set the ruleset FIRST so mod validation runs against the correct one.
                    if (beatmapInfo.Ruleset != null && !beatmapInfo.Ruleset.Equals(Ruleset.Value))
                        Ruleset.Value = beatmapInfo.Ruleset;

                    Beatmap.Value = working;

                    // SANITIZE MODS: drop any selected mod that doesn't belong to the target
                    // ruleset (this is what caused "Gameplay was started with a mod belonging to a
                    // ruleset different than 'osu!'"). We rebuild the selection from the ruleset's
                    // own mod instances, matched by acronym, and keep only a compatible set.
                    sanitizeSelectedModsForCurrentRuleset();

                    empyreanMenuMusic?.StopMenuTheme();

                    this.Push(new osu.Game.Empyrean.EmpyreanPlayerLoader(() => new osu.Game.Screens.Play.SoloPlayer()));
                }
                catch (Exception ex)
                {
                    Logger.Log($"EMPYREAN direct play failed: {ex.Message}", LoggingTarget.Runtime, LogLevel.Important);
                }
            }

            if (game != null)
                game.PerformFromScreen(_ => launch(), new[] { typeof(MainMenu) });
            else if (this.IsCurrentScreen())
                launch();
        }

        /// <summary>
        /// Rebuilds <see cref="OsuScreen.Mods"/> so it only contains mods that belong to the
        /// CURRENT ruleset. Selected mods are re-resolved from the ruleset's own mod instances by
        /// acronym (carrying over their settings where possible); anything that isn't a mod of the
        /// current ruleset is dropped, and the result is reduced to a compatible set. Prevents the
        /// "mod belonging to a ruleset different than ..." gameplay error.
        /// </summary>
        private void sanitizeSelectedModsForCurrentRuleset()
        {
            try
            {
                var rulesetInstance = Ruleset.Value?.CreateInstance();
                if (rulesetInstance == null)
                    return;

                // The set of mod TYPES valid for this ruleset.
                var availableTypes = rulesetInstance.CreateAllMods().Select(m => m.GetType()).ToHashSet();
                var rebuilt = new List<Mod>();

                foreach (var selected in Mods.Value)
                {
                    // Keep the EXACT selected instance if its type belongs to this ruleset — this
                    // preserves the user's adjusted settings (e.g. DT speed 1.1x). We only drop
                    // mods that are foreign to the ruleset (the cause of the cross-ruleset error).
                    if (availableTypes.Contains(selected.GetType()) && !rebuilt.Any(m => m.Acronym == selected.Acronym))
                        rebuilt.Add(selected);
                }

                // Reduce to a compatible set just in case.
                if (!ModUtils.CheckCompatibleSet(rebuilt, out _))
                    rebuilt = rebuilt.Where((m, i) => ModUtils.CheckCompatibleSet(rebuilt.Take(i + 1))).ToList();

                Mods.Value = rebuilt;
            }
            catch (Exception ex)
            {
                // On any doubt, clear mods entirely — a clean launch beats a crash.
                Logger.Log($"EMPYREAN mod sanitize failed, clearing mods: {ex.Message}", LoggingTarget.Runtime);
                Mods.Value = System.Array.Empty<Mod>();
            }
        }

        private void loadQuickPlay() => this.Push(new OnlinePlay.Matchmaking.Intro.ScreenIntro(MatchmakingPoolType.QuickPlay));

        private void loadRankedPlay() => this.Push(new OnlinePlay.Matchmaking.Intro.ScreenIntro(MatchmakingPoolType.RankedPlay));

        private void openDailyChallenge()
        {
            // Fetch today's daily-challenge room from the metadata service, then push the screen.
            var info = metadataClient?.DailyChallengeInfo?.Value;
            if (info == null)
            {
                loadSongSelect();
                return;
            }

            var req = new osu.Game.Online.Rooms.GetRoomRequest(info.Value.RoomID);
            req.Success += room => Schedule(() =>
            {
                if (this.IsCurrentScreen())
                    this.Push(new DailyChallenge(room));
            });
            req.Failure += _ => Schedule(loadSongSelect);
            api.Queue(req);
        }

        private partial class MobileDisclaimerDialog : PopupDialog
        {
            public MobileDisclaimerDialog(Action confirmed)
            {
                HeaderText = ButtonSystemStrings.MobileDisclaimerHeader;
                BodyText = ButtonSystemStrings.MobileDisclaimerBody;

                Icon = FontAwesome.Solid.SmileBeam;

                Buttons = new PopupDialogButton[]
                {
                    new PopupDialogOkButton
                    {
                        Text = ButtonSystemStrings.MobileDisclaimerOkButton,
                        Action = confirmed,
                    },
                };
            }
        }
    }
}
