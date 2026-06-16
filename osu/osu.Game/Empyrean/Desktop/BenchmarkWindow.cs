// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Utils;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A short, self-contained performance benchmark. It runs three load phases — heavy jumps,
    /// heavy streams, and heavy sliders/spinners — by driving a large synthetic scene of moving
    /// circles (the same draw + transform work gameplay produces), while sampling per-frame frame
    /// times. At the end it reports AVG FPS, MAX FPS, and the 1% and 0.1% low FPS.
    ///
    /// Note: this measures rendering/update throughput under representative load. It is not a replay
    /// of specific ranked maps, but it stresses the same pipeline (many transformed FastCircles,
    /// rotation, fades) and yields real frame-time percentiles for comparing builds and machines.
    /// </summary>
    public partial class BenchmarkWindow : Win95Window
    {
        private enum Phase
        {
            Idle,
            Jumps,
            Streams,
            SlidersSpinners,
            Done,
        }

        private Phase phase = Phase.Idle;
        private double phaseElapsed;
        private const double phase_duration = 4000; // ms per phase

        private readonly List<double> frameTimes = new List<double>();

        private Container scene;
        private Win95Button startButton;
        private OsuSpriteText phaseLabel;
        private FillFlowContainer results;

        // Reusable load actors.
        private readonly List<FastCircle> actors = new List<FastCircle>();

        public BenchmarkWindow()
            : base("EMPYREAN Benchmark", FontAwesome.Solid.Trophy)
        {
            Name = "EMPYREAN Benchmark";
            Size = new Vector2(640, 520);
        }

        [BackgroundDependencyLoader]
        private void load()
        {
            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.FACE },

                    // Header row with start button + phase label.
                    new FillFlowContainer
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 34,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(8, 0),
                        Padding = new MarginPadding(6),
                        Children = new Drawable[]
                        {
                            startButton = makeStartButton(),
                            phaseLabel = new OsuSpriteText
                            {
                                Anchor = Anchor.CentreLeft,
                                Origin = Anchor.CentreLeft,
                                Text = "Ready. Click Run to start the benchmark.",
                                Font = OsuFont.GetFont(size: 14),
                                Colour = Win95.TEXT,
                            },
                        },
                    },

                    // The synthetic load scene (black like gameplay).
                    new Container
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding { Top = 40, Bottom = 150, Left = 6, Right = 6 },
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.Black },
                            new Win95Bevel(Win95Bevel.Style.Field),
                            scene = new Container
                            {
                                RelativeSizeAxes = Axes.Both,
                                Masking = true,
                            },
                        },
                    },

                    // Results panel.
                    new Container
                    {
                        RelativeSizeAxes = Axes.X,
                        Height = 140,
                        Anchor = Anchor.BottomLeft,
                        Origin = Anchor.BottomLeft,
                        Padding = new MarginPadding(6),
                        Children = new Drawable[]
                        {
                            new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                            new Win95Bevel(Win95Bevel.Style.Field),
                            results = new FillFlowContainer
                            {
                                RelativeSizeAxes = Axes.Both,
                                Direction = FillDirection.Vertical,
                                Padding = new MarginPadding(8),
                                Spacing = new Vector2(0, 3),
                                Children = new Drawable[]
                                {
                                    new OsuSpriteText { Text = "Results will appear here after the run.", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT_DISABLED },
                                },
                            },
                        },
                    },
                },
            });
        }

        private Win95Button makeStartButton()
        {
            startButton = new Win95Button { Size = new Vector2(90, 24), Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft };
            startButton.Action = beginBenchmark;
            startButton.Add(new OsuSpriteText { Anchor = Anchor.Centre, Origin = Anchor.Centre, Text = "Run", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT });
            return startButton;
        }

        private void beginBenchmark()
        {
            if (phase != Phase.Idle && phase != Phase.Done)
                return;

            frameTimes.Clear();
            results.Clear();
            results.Add(new OsuSpriteText { Text = "Benchmarking… keep this window focused.", Font = OsuFont.GetFont(size: 13), Colour = Win95.TEXT });

            startPhase(Phase.Jumps);
        }

        private void startPhase(Phase next)
        {
            phase = next;
            phaseElapsed = 0;
            buildSceneForPhase(next);

            phaseLabel.Text = next switch
            {
                Phase.Jumps => "Phase 1/3 — heavy jumps",
                Phase.Streams => "Phase 2/3 — heavy streams",
                Phase.SlidersSpinners => "Phase 3/3 — heavy sliders & spinners",
                _ => "",
            };
        }

        private void buildSceneForPhase(Phase p)
        {
            scene.Clear();
            actors.Clear();

            // Spawn a heavy population of circles. Counts are deliberately large to stress the GPU
            // fill-rate and the per-frame transform/update work, the way dense maps do.
            int count = p switch
            {
                Phase.Jumps => 350,
                Phase.Streams => 800,
                Phase.SlidersSpinners => 500,
                _ => 0,
            };

            for (int i = 0; i < count; i++)
            {
                var c = new FastCircle
                {
                    Origin = Anchor.Centre,
                    Size = new Vector2(p == Phase.Jumps ? 70 : p == Phase.Streams ? 36 : 90),
                    Colour = new Color4(RNG.NextSingle(0.3f, 1f), RNG.NextSingle(0.3f, 1f), RNG.NextSingle(0.3f, 1f), 1f),
                    Alpha = 0.85f,
                    EdgeSmoothness = 0f, // flat, cheap edge — matches EMPYREAN gameplay
                    Position = new Vector2(RNG.NextSingle(0, 1), RNG.NextSingle(0, 1)),
                    RelativePositionAxes = Axes.Both,
                };
                scene.Add(c);
                actors.Add(c);
            }
        }

        protected override void Update()
        {
            base.Update();

            if (phase == Phase.Idle || phase == Phase.Done)
                return;

            double dt = Clock.ElapsedFrameTime;

            // Record frame time (skip absurd spikes from the very first frame of a phase).
            if (phaseElapsed > 50 && dt > 0 && dt < 1000)
                frameTimes.Add(dt);

            phaseElapsed += dt;

            animateActors();

            if (phaseElapsed >= phase_duration)
            {
                Phase nextPhase = phase switch
                {
                    Phase.Jumps => Phase.Streams,
                    Phase.Streams => Phase.SlidersSpinners,
                    _ => Phase.Done,
                };

                if (nextPhase == Phase.Done)
                    finishBenchmark();
                else
                    startPhase(nextPhase);
            }
        }

        private void animateActors()
        {
            // Move/rotate/pulse every actor each frame to generate real per-frame work.
            float t = (float)(phaseElapsed / 1000.0);

            for (int i = 0; i < actors.Count; i++)
            {
                var a = actors[i];
                float seed = i * 0.137f;

                switch (phase)
                {
                    case Phase.Jumps:
                        // Teleporting positions (jump pattern) every ~120ms.
                        a.Position = new Vector2(
                            0.5f + 0.45f * MathF.Sin(seed * 12.9898f + MathF.Floor(t * 8) * 1.7f),
                            0.5f + 0.45f * MathF.Cos(seed * 78.233f + MathF.Floor(t * 8) * 2.3f));
                        break;

                    case Phase.Streams:
                        // Fast continuous sweep (stream pattern).
                        a.Position = new Vector2(
                            0.5f + 0.45f * MathF.Sin(t * 6f + seed * 20f),
                            0.5f + 0.45f * MathF.Cos(t * 6f + seed * 20f));
                        break;

                    case Phase.SlidersSpinners:
                        // Large rotating/scaling discs (slider/spinner pattern).
                        a.Rotation = (t * 180f + seed * 360f) % 360f;
                        float pulse = 0.8f + 0.2f * MathF.Sin(t * 4f + seed * 6f);
                        a.Scale = new Vector2(pulse);
                        a.Position = new Vector2(
                            0.5f + 0.4f * MathF.Sin(t * 1.5f + seed * 3f),
                            0.5f + 0.4f * MathF.Cos(t * 1.2f + seed * 3f));
                        break;
                }
            }
        }

        private void finishBenchmark()
        {
            phase = Phase.Done;
            scene.Clear();
            actors.Clear();
            phaseLabel.Text = "Benchmark complete.";

            results.Clear();

            if (frameTimes.Count < 10)
            {
                results.Add(new OsuSpriteText { Text = "Not enough samples collected.", Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT });
                return;
            }

            // Convert frame times (ms) to FPS percentiles. 1% low = mean of the slowest 1% of frames.
            var sorted = new List<double>(frameTimes);
            sorted.Sort(); // ascending frame time (fast -> slow)

            double avgFrameTime = mean(sorted, 0, sorted.Count);
            double avgFps = 1000.0 / avgFrameTime;

            double minFrameTime = sorted[0];
            double maxFps = 1000.0 / minFrameTime;

            // Slowest frames are at the end of the sorted list.
            int onePercentCount = Math.Max(1, sorted.Count / 100);
            int tenthPercentCount = Math.Max(1, sorted.Count / 1000);

            double onePercentLowFrameTime = mean(sorted, sorted.Count - onePercentCount, sorted.Count);
            double tenthPercentLowFrameTime = mean(sorted, sorted.Count - tenthPercentCount, sorted.Count);

            double onePercentLowFps = 1000.0 / onePercentLowFrameTime;
            double tenthPercentLowFps = 1000.0 / tenthPercentLowFrameTime;

            results.Add(new OsuSpriteText { Text = "EMPYREAN Benchmark Results", Font = OsuFont.GetFont(size: 16, weight: FontWeight.Bold), Colour = Win95.TITLE });
            results.Add(resultRow("Average FPS", $"{avgFps:0}"));
            results.Add(resultRow("Maximum FPS", $"{maxFps:0}"));
            results.Add(resultRow("1% low FPS", $"{onePercentLowFps:0}"));
            results.Add(resultRow("0.1% low FPS", $"{tenthPercentLowFps:0}"));
            results.Add(new OsuSpriteText { Text = $"({frameTimes.Count} frames sampled across 3 phases)", Font = OsuFont.GetFont(size: 11), Colour = Win95.TEXT_DISABLED });
        }

        private static double mean(List<double> values, int start, int end)
        {
            if (end <= start) return values.Count > 0 ? values[0] : 1;
            double sum = 0;
            for (int i = start; i < end; i++)
                sum += values[i];
            return sum / (end - start);
        }

        private Drawable resultRow(string label, string value) => new Container
        {
            RelativeSizeAxes = Axes.X,
            AutoSizeAxes = Axes.Y,
            Children = new Drawable[]
            {
                new OsuSpriteText { Text = label, Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                new OsuSpriteText { Anchor = Anchor.TopRight, Origin = Anchor.TopRight, Text = value, Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold), Colour = Win95.TEXT },
            },
        };
    }
}
