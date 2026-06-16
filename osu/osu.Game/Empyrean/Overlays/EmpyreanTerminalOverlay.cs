// Copyright (c) ppy Pty Ltd <contact@ppy.sh>. Licensed under the MIT Licence.
// See the LICENCE file in the repository root for full licence text.

using System;
using System.Collections.Generic;
using osu.Framework.Allocation;
using osu.Framework.Graphics;
using osu.Framework.Graphics.Containers;
using osu.Framework.Graphics.Shapes;
using osu.Framework.Graphics.Sprites;
using osu.Framework.Graphics.UserInterface;
using osu.Framework.Input.Events;
using osu.Game.Empyrean.Terminal;
using osu.Game.Empyrean.UI;
using osu.Game.Graphics;
using osu.Game.Graphics.Sprites;
using osuTK.Input;

namespace osu.Game.Empyrean.Overlays
{
    /// <summary>
    /// EMPYREAN MS-DOS-style command console (PROJECT.md §6).
    ///
    /// Design constraints honoured:
    ///  - extremely light rendering: a black box, fixed-width text, a flat Win95 frame. No
    ///    masking chains beyond the cheap bevel, no animated chrome, no shadows, no blur.
    ///  - zero per-frame work; the console only reacts to key/submit events.
    ///  - the engine (parsing + command dispatch) is fully decoupled (<see cref="TerminalEngine"/>)
    ///    and unit tested separately, so this drawable stays a thin view layer.
    ///
    /// Toggled by the backtick / tilde key (classic quake/DOS-console convention). We keep
    /// non-positional input propagating even while hidden so the toggle key always reaches us.
    /// </summary>
    public partial class EmpyreanTerminalOverlay : VisibilityContainer
    {
        private const int max_output_lines = 400;

        private readonly ITerminalContext context;
        private TerminalEngine engine = null!;

        private FillFlowContainer outputFlow = null!;
        private BasicScrollContainer outputScroll = null!;
        private ConsoleInput input = null!;

        private readonly List<string> history = new List<string>();
        private int historyIndex;

        // Fixed-width text using the default registered font (no external monospace dependency).
        private static FontUsage mono(float size = 16) => OsuFont.Default.With(fixedWidth: true, size: size);

        public EmpyreanTerminalOverlay(ITerminalContext context)
        {
            this.context = context;
            RelativeSizeAxes = Axes.Both;
            // Drop-down console occupying the top half of the screen.
            Height = 0.5f;
        }

        // Keep the toggle key reachable even while the console is hidden.
        public override bool PropagateNonPositionalInputSubTree => true;

        [BackgroundDependencyLoader]
        private void load()
        {
            engine = TerminalCommands.Build(context);

            Child = new Win95Panel
            {
                RelativeSizeAxes = Axes.Both,
                Child = new Container
                {
                    RelativeSizeAxes = Axes.Both,
                    Children = new Drawable[]
                    {
                        // Win95 navy title bar.
                        new Container
                        {
                            RelativeSizeAxes = Axes.X,
                            Height = Win95.TITLE_HEIGHT,
                            Children = new Drawable[]
                            {
                                new Box { RelativeSizeAxes = Axes.Both, Colour = osu.Framework.Graphics.Colour.ColourInfo.GradientHorizontal(Win95.VW_PURPLE, Win95.VW_MAGENTA) },
                                new OsuSpriteText
                                {
                                    Text = "EMPYREAN — console",
                                    Colour = Win95.TITLE_TEXT,
                                    Font = OsuFont.GetFont(size: 14, weight: FontWeight.Bold),
                                    Anchor = Anchor.CentreLeft,
                                    Origin = Anchor.CentreLeft,
                                    Margin = new MarginPadding { Left = 4 },
                                },
                            },
                        },
                        // Black DOS console surface inside a sunken bevel.
                        new Container
                        {
                            RelativeSizeAxes = Axes.Both,
                            Padding = new MarginPadding { Top = Win95.TITLE_HEIGHT },
                            Child = new Win95Panel(sunken: true)
                            {
                                RelativeSizeAxes = Axes.Both,
                                Child = new Container
                                {
                                    RelativeSizeAxes = Axes.Both,
                                    Children = new Drawable[]
                                    {
                                        new Box { RelativeSizeAxes = Axes.Both, Colour = Win95.TERMINAL_BG },
                                        outputScroll = new BasicScrollContainer
                                        {
                                            RelativeSizeAxes = Axes.Both,
                                            Padding = new MarginPadding { Bottom = 22, Left = 4, Right = 4, Top = 2 },
                                            ScrollbarVisible = false,
                                            Child = outputFlow = new FillFlowContainer
                                            {
                                                RelativeSizeAxes = Axes.X,
                                                AutoSizeAxes = Axes.Y,
                                                Direction = FillDirection.Vertical,
                                            },
                                        },
                                        input = new ConsoleInput
                                        {
                                            RelativeSizeAxes = Axes.X,
                                            Height = 20,
                                            Anchor = Anchor.BottomLeft,
                                            Origin = Anchor.BottomLeft,
                                            OnSubmit = submit,
                                            OnHistoryUp = () => recallHistory(-1),
                                            OnHistoryDown = () => recallHistory(1),
                                            OnComplete = autocomplete,
                                        },
                                    },
                                },
                            },
                        },
                    },
                },
            };

            foreach (string line in EmpyreanInfo.Banner.Split('\n'))
                print(line);
        }

        private void print(string text)
        {
            if (text == "\f")
            {
                outputFlow.Clear();
                return;
            }

            outputFlow.Add(new OsuSpriteText
            {
                Text = text,
                Font = mono(),
                Colour = Win95.TERMINAL_FG,
            });

            if (outputFlow.Count > max_output_lines)
                outputFlow.Remove(outputFlow[0], true);

            outputScroll.ScrollToEnd();
        }

        private void submit(string line)
        {
            print($"> {line}");

            if (!string.IsNullOrWhiteSpace(line))
            {
                history.Add(line);
                historyIndex = history.Count;
            }

            foreach (string outLine in engine.Run(line))
                print(outLine);
        }

        private void recallHistory(int direction)
        {
            if (history.Count == 0)
                return;

            historyIndex = Math.Clamp(historyIndex + direction, 0, history.Count);
            input.Text = historyIndex < history.Count ? history[historyIndex] : string.Empty;
        }

        private void autocomplete()
        {
            var matches = engine.Complete(input.Text);
            if (matches.Count == 1)
                input.Text = matches[0] + " ";
            else if (matches.Count > 1)
                print("  " + string.Join("  ", matches));
        }

        protected override void PopIn()
        {
            this.FadeIn();
            input.TakeFocus();
        }

        protected override void PopOut()
        {
            this.FadeOut();
            input.ReleaseConsoleFocus();
        }

        protected override bool OnKeyDown(KeyDownEvent e)
        {
            // Backtick / tilde toggles the console (osuTK names this key Tilde / Grave).
            if (e.Key == Key.Tilde)
            {
                ToggleVisibility();
                return true;
            }

            if (e.Key == Key.Escape && State.Value == Visibility.Visible)
            {
                Hide();
                return true;
            }

            return base.OnKeyDown(e);
        }

        /// <summary>
        /// Minimal single-line console input. Subclasses <see cref="BasicTextBox"/> to inherit
        /// caret + text editing for free, but intercepts Enter / arrows / Tab for console behaviour.
        /// </summary>
        private partial class ConsoleInput : BasicTextBox
        {
            public Action<string>? OnSubmit;
            public Action? OnHistoryUp;
            public Action? OnHistoryDown;
            public Action? OnComplete;

            public ConsoleInput()
            {
                BackgroundUnfocused = Win95.TERMINAL_BG;
                BackgroundFocused = Win95.TERMINAL_BG;
                TextContainer.Height = 0.7f;
                // Behave like a real terminal: keep focus across submits.
                ReleaseFocusOnCommit = false;
                CommitOnFocusLost = false;
            }

            public void TakeFocus() => GetContainingFocusManager()?.ChangeFocus(this);
            public void ReleaseConsoleFocus() => GetContainingFocusManager()?.ChangeFocus(null);

            protected override void OnTextCommitted(bool textChanged)
            {
                string committed = Text;
                Text = string.Empty;
                base.OnTextCommitted(textChanged);
                OnSubmit?.Invoke(committed);
            }

            protected override bool OnKeyDown(KeyDownEvent e)
            {
                switch (e.Key)
                {
                    case Key.Up:
                        OnHistoryUp?.Invoke();
                        return true;

                    case Key.Down:
                        OnHistoryDown?.Invoke();
                        return true;

                    case Key.Tab:
                        OnComplete?.Invoke();
                        return true;
                }

                return base.OnKeyDown(e);
            }
        }
    }
}
