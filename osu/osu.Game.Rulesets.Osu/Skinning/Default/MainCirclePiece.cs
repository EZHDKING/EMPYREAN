// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using osu.Framework.Allocation;
using osu.Framework.Bindables;
using osu.Framework.Extensions.ObjectExtensions;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Game.Configuration;
using osu.Game.Rulesets.Objects.Drawables;
using osu.Game.Rulesets.Osu.Objects;
using osu.Game.Rulesets.Osu.Objects.Drawables;
using osuTK.Graphics;

namespace osu.Game.Rulesets.Osu.Skinning.Default
{
    public partial class MainCirclePiece : CompositeDrawable
    {
        // Full (upstream) layers. Some are null in EMPYREAN flat mode.
        private CirclePiece? circle;
        private RingPiece? ring;
        private FlashPiece? flash;
        private ExplodePiece? explode;
        private NumberPiece? number;
        private GlowPiece? glow;

        // EMPYREAN flat-mode layers.
        private Box? flatFill;
        private RingPiece? flatRing;
        private Box? flatFlash;

        private bool flatMode;

        public MainCirclePiece()
        {
            Size = OsuHitObject.OBJECT_DIMENSIONS;

            Anchor = Anchor.Centre;
            Origin = Anchor.Centre;
        }

        private readonly IBindable<Color4> accentColour = new Bindable<Color4>();
        private readonly IBindable<int> indexInCurrentCombo = new Bindable<int>();

        [Resolved]
        private DrawableHitObject drawableObject { get; set; } = null!;

        [BackgroundDependencyLoader]
        private void load(OsuConfigManager? config)
        {
            // EMPYREAN: minimal, low-overdraw hit circle. The default upstream piece stacks
            // six masked/additive layers (glow + disc + animated triangles + kiai flash + ring +
            // flash + explode + number). Each masked circle is a fragment-shader distance pass and
            // every additive layer is extra overdraw. For top-level play none of that decoration
            // matters — readability does. So flat mode draws exactly three cheap things:
            //   1. one solid masked circle (the body),
            //   2. one bordered ring (edge definition),
            //   3. the combo number (kept; competitive players read it),
            // plus a single non-additive white flash on hit. No glow, no triangles, no explode.
            // This is the headline gameplay-path GPU win described in PROJECT.md §11 / §28.
            flatMode = config?.Get<bool>(OsuSetting.EmpyreanFlatGameplay) ?? true;

            if (flatMode)
            {
                InternalChildren = new Drawable[]
                {
                    new CircularContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Masking = true,
                        // CornerExponent 2 == true rounded circle in the SDF masking shader.
                        // We keep masking (it is the cheapest way to get a circle here) but avoid
                        // any inner sprite/texture sampling and any additive children.
                        Child = flatFill = new Box
                        {
                            RelativeSizeAxes = Axes.Both,
                        },
                    },
                    flatRing = new RingPiece(),
                    number = new NumberPiece(),
                    flatFlash = new Box
                    {
                        RelativeSizeAxes = Axes.Both,
                        Anchor = Anchor.Centre,
                        Origin = Anchor.Centre,
                        Colour = Color4.White,
                        Alpha = 0,
                        // NOTE: deliberately NOT additive — a normal-blended flash is one cheap
                        // draw versus an additive pass that forces a blend-state change.
                    },
                };
            }
            else
            {
                InternalChildren = new Drawable[]
                {
                    glow = new GlowPiece(),
                    circle = new CirclePiece(),
                    number = new NumberPiece(),
                    ring = new RingPiece(),
                    flash = new FlashPiece(),
                    explode = new ExplodePiece(),
                };
            }

            var drawableOsuObject = (DrawableOsuHitObject)drawableObject;

            accentColour.BindTo(drawableObject.AccentColour);
            indexInCurrentCombo.BindTo(drawableOsuObject.IndexInCurrentComboBindable);
        }

        protected override void LoadComplete()
        {
            base.LoadComplete();

            accentColour.BindValueChanged(colour =>
            {
                if (flatMode)
                {
                    if (flatFill != null) flatFill.Colour = colour.NewValue;
                }
                else
                {
                    explode!.Colour = colour.NewValue;
                    glow!.Colour = colour.NewValue;
                    circle!.Colour = colour.NewValue;
                }
            }, true);

            indexInCurrentCombo.BindValueChanged(index => number!.Text = (index.NewValue + 1).ToString(), true);

            drawableObject.ApplyCustomUpdateState += updateStateTransforms;
            updateStateTransforms(drawableObject, drawableObject.State.Value);
        }

        private void updateStateTransforms(DrawableHitObject drawableHitObject, ArmedState state)
        {
            if (flatMode)
            {
                updateFlatStateTransforms(state);
                return;
            }

            using (BeginAbsoluteSequence(drawableObject.StateUpdateTime))
                glow!.FadeOut(400);

            using (BeginAbsoluteSequence(drawableObject.HitStateUpdateTime))
            {
                switch (state)
                {
                    case ArmedState.Hit:
                        const double flash_in = 40;
                        const double flash_out = 100;

                        flash!.FadeTo(0.8f, flash_in)
                              .Then()
                              .FadeOut(flash_out);

                        explode!.FadeIn(flash_in);
                        this.ScaleTo(1.5f, 400, Easing.OutQuad);

                        using (BeginDelayedSequence(flash_in))
                        {
                            ring!.FadeOut();
                            circle!.FadeOut();
                            number!.FadeOut();

                            this.FadeOut(800);
                        }

                        break;
                }
            }
        }

        private void updateFlatStateTransforms(ArmedState state)
        {
            using (BeginAbsoluteSequence(drawableObject.HitStateUpdateTime))
            {
                switch (state)
                {
                    case ArmedState.Hit:
                        // Minimal, readable hit feedback: a short white flash + quick fade.
                        // No scale-up explosion, no particle work — cheapest acceptable cue.
                        const double flash_in = 40;

                        flatFlash!.FadeTo(0.7f, flash_in).Then().FadeOut(120);

                        using (BeginDelayedSequence(flash_in))
                        {
                            flatRing!.FadeOut();
                            flatFill!.FadeOut();
                            number!.FadeOut();
                            this.FadeOut(220);
                        }

                        break;
                }
            }
        }

        protected override void Dispose(bool isDisposing)
        {
            base.Dispose(isDisposing);

            if (drawableObject.IsNotNull())
                drawableObject.ApplyCustomUpdateState -= updateStateTransforms;
        }
    }
}
