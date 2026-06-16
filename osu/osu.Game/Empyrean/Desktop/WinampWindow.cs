// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Rendering;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osu.Game.Overlays;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// An authentic classic-WinAmp player. It composites the real WinAmp main window from a loaded
    /// .wsz skin's sprite sheets (Main.bmp background, Cbuttons.bmp transport buttons), drives the
    /// game's real <see cref="MusicController"/>, and lets the user switch between bundled skins.
    /// The standard WinAmp main window is 275x116; we scale it up for legibility.
    /// </summary>
    public partial class WinampWindow : Win95Window
    {
        // Authentic WinAmp classic main-window dimensions.
        private const float skin_w = 275f;
        private const float skin_h = 116f;
        private const float scale = 2f;

        [Resolved(canBeNull: true)]
        private MusicController music { get; set; }

        [Resolved(canBeNull: true)]
        private osu.Framework.Bindables.Bindable<WorkingBeatmap> beatmap { get; set; }

        [Resolved(canBeNull: true)]
        private IRenderer renderer { get; set; }

        private Container skinSurface;
        private OsuSpriteText titleText;
        private OsuSpriteText timeText;
        private Box positionFill;

        private WinampSkin skin;
        private int skinIndex;
        private float scrollOffset;
        private string fullTitle = "EMPYREAN WINAMP";

        public WinampWindow()
            : base("WinAmp", FontAwesome.Solid.Music)
        {
            Name = "WinAmp";
            // Window is the scaled skin plus the Win95 title bar / borders.
            Size = new Vector2(skin_w * scale + 12, skin_h * scale + 40);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black },
                    skinSurface = new Container
                    {
                        Anchor = Anchor.TopCentre,
                        Origin = Anchor.TopCentre,
                        Size = new Vector2(skin_w * scale, skin_h * scale),
                    },
                    // Skin switcher button (bottom strip).
                    Win95Button.Text("Skin »", cycleSkin, 70, 22).With(b =>
                    {
                        b.Anchor = Anchor.BottomLeft;
                        b.Origin = Anchor.BottomLeft;
                    }),
                },
            });

            loadSkin(0);

            // Bind to the beatmap bindable so the title reflects the ACTUAL current track. Using
            // MusicController.TrackChanged here fires before the beatmap bindable updates, which is
            // what made WinAmp show "one song behind".
            if (beatmap != null)
                beatmap.BindValueChanged(_ => Schedule(updateTitle), true);
            else
                updateTitle();
        }

        private void cycleSkin()
        {
            skinIndex = (skinIndex + 1) % WinampSkin.BundledSkins.Length;
            loadSkin(skinIndex);
        }

        private void loadSkin(int index)
        {
            skin = WinampSkin.Load(WinampSkin.BundledSkins[index].id, renderer);
            buildSkinSurface();
        }

        private void buildSkinSurface()
        {
            skinSurface.Clear();

            if (skin?.Main == null)
            {
                // Fallback: flat dark panel with a label so the player still works without a skin.
                skinSurface.Add(new Box { RelativeSizeAxes = Axes.Both, Colour = new Color4(24, 24, 32, 255) });
                addControls(fallback: true);
                return;
            }

            // The main background fills the whole skin surface.
            skinSurface.Add(new Sprite
            {
                RelativeSizeAxes = Axes.Both,
                Texture = skin.Main,
            });

            addControls(fallback: false);
        }

        private void addControls(bool fallback)
        {
            // LCD time + scrolling title overlaid on the skin's display region.
            // WinAmp time display sits around (36,26); title around (111,27) in the 275x116 skin.
            timeText = new OsuSpriteText
            {
                Position = new Vector2(39 * scale, 26 * scale),
                Font = OsuFont.GetFont(size: 18, weight: FontWeight.Bold),
                Colour = new Color4(0, 255, 120, 255),
                Text = "0:00",
            };

            var titleClip = new Container
            {
                Position = new Vector2(110 * scale, 25 * scale),
                Size = new Vector2(150 * scale, 9 * scale),
                Masking = true,
                Child = titleText = new OsuSpriteText
                {
                    Font = OsuFont.GetFont(size: 13, weight: FontWeight.Bold),
                    Colour = new Color4(0, 255, 120, 255),
                    Text = fullTitle,
                },
            };

            skinSurface.Add(timeText);
            skinSurface.Add(titleClip);

            // Transport buttons. In a real skin these are regions of Cbuttons.bmp; we overlay
            // invisible click targets at the authentic button positions (x≈16..136, y≈88) so the
            // visible skin art is what the user sees and our hit areas line up with it.
            // Button strip starts at x=16, y=88 in the 275x116 main window; each ~23px wide, 18 tall.
            float by = 88 * scale;
            float bh = 18 * scale;
            addButton(16, by, 23, bh, () => music?.PreviousTrack());
            addButton(39, by, 23, bh, playPressed);
            addButton(62, by, 23, bh, () => music?.TogglePause());
            addButton(85, by, 23, bh, () => music?.Stop(true));
            addButton(108, by, 22, bh, () => music?.NextTrack());
            addButton(136, by, 22, 16 * scale, () => OnClose?.Invoke()); // eject

            // Position bar fill (authentic posbar sits at y≈72, x≈16..291).
            skinSurface.Add(new Container
            {
                Position = new Vector2(16 * scale, 72 * scale),
                Size = new Vector2(248 * scale, 10 * scale),
                Children = new Drawable[]
                {
                    positionFill = new Box
                    {
                        RelativeSizeAxes = Axes.Y,
                        Width = 0,
                        Colour = new Color4(0, 200, 90, fallback ? (byte)255 : (byte)120),
                    },
                },
            });

            if (fallback)
            {
                // Label so it's clear which player this is when no skin art is present.
                skinSurface.Add(new OsuSpriteText
                {
                    Anchor = Anchor.TopCentre,
                    Origin = Anchor.TopCentre,
                    Y = 6,
                    Text = "WINAMP",
                    Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold),
                    Colour = new Color4(0, 255, 120, 255),
                });
            }
        }

        private void addButton(float x, float y, float w, float h, Action action)
        {
            skinSurface.Add(new ClickTarget(action)
            {
                Position = new Vector2(x * scale, y),
                Size = new Vector2(w * scale, h),
            });
        }

        private void playPressed()
        {
            if (music == null)
                return;

            try
            {
                music.CurrentTrack.Volume.Value = 1;
            }
            catch { }

            music.Play(requestedByUser: true);
        }

        private void updateTitle()
        {
            fullTitle = beatmap?.Value?.BeatmapInfo?.Metadata == null
                ? "EMPYREAN WINAMP — no track"
                : $"{beatmap.Value.Metadata.Artist} - {beatmap.Value.Metadata.Title}";
            if (titleText != null)
                titleText.Text = fullTitle;
            scrollOffset = 0;
        }

        protected override void Update()
        {
            base.Update();

            var track = music?.CurrentTrack;
            if (track != null && track.Length > 0)
            {
                double pos = track.CurrentTime;
                if (positionFill != null)
                    positionFill.Width = (float)Math.Clamp(pos / track.Length, 0, 1);

                if (timeText != null)
                {
                    var ts = TimeSpan.FromMilliseconds(pos);
                    timeText.Text = $"{(int)ts.TotalMinutes}:{ts.Seconds:00}";
                }
            }

            if (titleText != null && titleText.DrawWidth > 0)
            {
                scrollOffset += (float)(Time.Elapsed * 0.03);
                float range = titleText.DrawWidth + 30;
                titleText.X = -((scrollOffset % range));
            }
        }

        /// <summary>An invisible click/seek target overlaid on the skin art.</summary>
        private partial class ClickTarget : Container
        {
            private readonly Action action;

            public ClickTarget(Action action)
            {
                this.action = action;
                // Fully transparent but still receives input.
                Child = new Box { RelativeSizeAxes = Axes.Both, Alpha = 0, AlwaysPresent = true };
            }

            protected override bool OnClick(ClickEvent e)
            {
                action?.Invoke();
                return true;
            }
        }
    }
}
