// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using System.Linq;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Input.Events;
using osu.Game.Beatmaps;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK;
using osuTK.Graphics;

namespace osu.Game.Empyrean.Desktop
{
    /// <summary>
    /// A Windows 95 prompt that lists the difficulties of a beatmap set. Double-clicking a
    /// difficulty launches it straight into gameplay (no modern song select). This is the
    /// "open a map -> pick a difficulty -> play instantly" flow.
    /// </summary>
    public partial class DifficultyPickerWindow : Win95Window
    {
        private readonly IBeatmapSetInfo set;
        private readonly Action<BeatmapInfo> onPlay;
        private readonly Action<BeatmapInfo> onCreateShortcut;
        private FillFlowContainer flow;

        public DifficultyPickerWindow(IBeatmapSetInfo set, Action<BeatmapInfo> onPlay, Action<BeatmapInfo> onCreateShortcut = null)
            : base(titleFor(set), FontAwesome.Solid.Play)
        {
            this.set = set;
            this.onPlay = onPlay;
            this.onCreateShortcut = onCreateShortcut;
            Name = titleFor(set);
            Size = new Vector2(440, 320);
        }

        private static string titleFor(IBeatmapSetInfo set)
        {
            string t = set?.Metadata?.Title ?? "beatmap";
            return $"{t} — choose difficulty";
        }

        [osu.Framework.Allocation.BackgroundDependencyLoader]
        private void load()
        {
            flow = new FillFlowContainer
            {
                RelativeSizeAxes = Axes.X,
                AutoSizeAxes = Axes.Y,
                Direction = FillDirection.Vertical,
            };

            Add(new Container
            {
                RelativeSizeAxes = Axes.Both,
                Children = new Drawable[]
                {
                    new Box { RelativeSizeAxes = Axes.Both, Colour = Color4.White },
                    new Win95Bevel(Win95Bevel.Style.Field),
                    new BasicScrollContainer
                    {
                        RelativeSizeAxes = Axes.Both,
                        Padding = new MarginPadding(3),
                        ScrollbarVisible = true,
                        Child = flow,
                    },
                },
            });

            // Order difficulties by star rating ascending (the natural Win95 list order).
            IEnumerable<IBeatmapInfo> diffs = set?.Beatmaps ?? Enumerable.Empty<IBeatmapInfo>();
            diffs = diffs.OrderBy(b => b.StarRating);

            int i = 0;
            foreach (var diff in diffs)
            {
                if (diff is BeatmapInfo bi)
                {
                    var captured = bi;
                    var rowRef = new DifficultyRow(bi, i++ % 2 == 0, () =>
                    {
                        onPlay?.Invoke(captured);
                        OnClose?.Invoke();
                        Expire();
                    }, () => onCreateShortcut?.Invoke(captured));

                    rowRef.OnSelected = () => selectRow(rowRef);
                    flow.Add(rowRef);

                    // Select the first row by default so Enter works immediately.
                    if (selectedRow == null)
                        selectRow(rowRef);
                }
            }

            if (flow.Children.Count == 0)
                flow.Add(new OsuSpriteText { Text = "  (no difficulties found)", Colour = Win95.TEXT, Font = OsuFont.GetFont(size: 14), Margin = new MarginPadding(6) });
        }

        private DifficultyRow selectedRow;

        private void selectRow(DifficultyRow row)
        {
            foreach (var r in flow.Children.OfType<DifficultyRow>())
                r.SetSelected(r == row);
            selectedRow = row;
        }

        // Enter plays the selected difficulty (so you can pick with the mouse/keys and press Enter).
        protected override bool OnKeyDown(KeyDownEvent e)
        {
            if (e.Key == osuTK.Input.Key.Enter || e.Key == osuTK.Input.Key.KeypadEnter)
            {
                selectedRow?.Play();
                return true;
            }

            return base.OnKeyDown(e);
        }

        private partial class DifficultyRow : Container
        {
            private readonly Action play;
            private readonly Action createShortcut;
            private readonly Box hover;
            private readonly Box selection;
            private readonly OsuSpriteText text;
            private bool selected;

            public Action OnSelected;

            public void Play() => play?.Invoke();

            public void SetSelected(bool value)
            {
                selected = value;
                selection.Alpha = value ? 1 : 0;
                text.Colour = value ? Color4.White : Win95.TEXT;
            }

            public DifficultyRow(BeatmapInfo diff, bool alt, Action play, Action createShortcut)
            {
                this.play = play;
                this.createShortcut = createShortcut;
                RelativeSizeAxes = Axes.X;
                Height = 24;

                string label = $"[{diff.Ruleset?.ShortName ?? "osu"}] {diff.DifficultyName}  ({diff.StarRating:0.00}★)";

                Children = new Drawable[]
                {
                    selection = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    hover = new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TITLE, Alpha = 0 },
                    new FillFlowContainer
                    {
                        Anchor = Anchor.CentreLeft,
                        Origin = Anchor.CentreLeft,
                        AutoSizeAxes = Axes.X,
                        RelativeSizeAxes = Axes.Y,
                        Direction = FillDirection.Horizontal,
                        Spacing = new Vector2(5, 0),
                        Margin = new MarginPadding { Left = 4 },
                        Children = new Drawable[]
                        {
                            new SpriteIcon { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Size = new Vector2(13), Icon = FontAwesome.Solid.Play, Colour = Win95.TITLE },
                            text = new OsuSpriteText { Anchor = Anchor.CentreLeft, Origin = Anchor.CentreLeft, Text = label, Font = OsuFont.GetFont(size: 14), Colour = Win95.TEXT },
                        },
                    },
                };
            }

            protected override bool OnHover(HoverEvent e)
            {
                if (!selected) hover.Alpha = 0.5f;
                return true;
            }

            protected override void OnHoverLost(HoverLostEvent e)
            {
                hover.Alpha = 0;
            }

            protected override bool OnClick(ClickEvent e)
            {
                OnSelected?.Invoke();
                return true;
            }

            protected override bool OnDoubleClick(DoubleClickEvent e)
            {
                play?.Invoke();
                return true;
            }

            protected override bool OnMouseDown(MouseDownEvent e)
            {
                // Right-click a difficulty to create a persistent desktop shortcut to it.
                if (e.Button == osuTK.Input.MouseButton.Right)
                {
                    createShortcut?.Invoke();
                    return true;
                }

                return base.OnMouseDown(e);
            }
        }
    }
}
